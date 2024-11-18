using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;



namespace Byn.Awrtc.Unity
{
    /// <summary>
    /// Base class for several individual converts that can receive an
    /// IFrame as input and then output Texture2D using various formats and
    /// methods to do the conversion.
    /// 
    /// This is still in early development.
    /// The goal is to automate any processing & converting of frames to Texture2D 
    /// no matter which IFrame format or platform is used.
    /// 
    /// 
    /// TODO: Remove EnsureTex. Replaced with Allocate
    /// TODO: Force completion on exit to avoid memory leaks on exit
    /// TODO: Fix issue in C++ component that triggers a frame multiple times if
    /// not processed on the first update.
    /// </summary>
    public abstract class AFrameConverter : IDisposable
    {
        private bool disposedValue;

        public abstract bool IsValidInput(IFrame frame);
        public abstract void EnsureTex(int image_width, int image_height, ref Texture2D tex);

        /// <summary>
        /// The converter will allocate everything needed to convert the frame and store a reference.
        /// The Texture can be null or an old texture to reuse. If the given texture is not
        /// suitable it will be destroyed and a new texture will be created in its place.
        /// </summary>
        /// <param name="from">
        /// Source for the new Texture. IFrame must not be Disposed until Complete is called.
        /// The converter will not own / dispose the IFrame.
        /// </param>
        /// <param name="tex">
        /// A texture to reuse (or replace)
        /// </param>
        public abstract void Allocate(IFrame from, ref Texture2D tex);
        /// <summary>
        /// Starts the conversion. This could possibly happen in a different thread or
        /// entirely different plugin. Poll IsDone to check when the process completed.
        /// In most cases Convert is single threaded and finishes the conversion synchronously.
        /// 
        /// </summary>
        public abstract void Convert();
        /// <summary>
        /// Call once IsDone is true. This returns the final Texture
        /// </summary>
        /// <returns>
        /// The final Texture (usually same reference as in Allocate). 
        /// The caller is responsible for disposing the texture or can recycle it in
        /// another Allocate call later.
        /// </returns>
        public abstract Texture2D Complete();
        /// <summary>
        /// Returns true once the conversion completed and Complete can be called.
        /// Reset to false after Complete was called
        /// </summary>
        public abstract bool IsDone { get; }
        /// <summary>
        /// Returns null if no special material is needed to use the Texture.
        /// Returns a material name if a custom material is needed to render the image.
        /// (so far only used for converters that output I420p as R8 textures)
        /// </summary>
        public abstract string MaterialName
        {
            get;
        }

        /// <summary>
        /// Used to detect memory leaks (Disposed not called)
        /// </summary>
        ~AFrameConverter()
        {
             Dispose(false);
        }

        public static void EnsureTex(int texture_width, int texture_height, TextureFormat format, ref Texture2D tex)
        {
            if (tex != null && (tex.width != texture_width || tex.height != texture_height || tex.format != format))
            {
                Texture2D.Destroy(tex);
                tex = null;
            }

            if (tex == null)
            {
                tex = new Texture2D(texture_width, texture_height, format, false, false);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //handled in subclasses
                }
                else
                {
                    //if we get here the converter was leaked and we might have leaked
                    //some temporary allocated textures / materials
                    Debug.LogWarning("Converter of type " + this.GetType().Name + " was not disposed. This can lead to memory leaks!");
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Receives ABGR (default native output format) and converts it into
    /// RGBA32 Texture2D.
    /// 
    /// This is essentially just a copy and the most stable & reliable converter
    /// but slow.
    /// </summary>
    public class FrameConverter_ABGR_to_RGBA32 : AFrameConverter
    {
        public readonly TextureFormat mOutputFormat = TextureFormat.RGBA32;

        private IFrame processing_from;
        private Texture2D processing_to;


        protected bool mIsDone = false;
        public override bool IsDone
        {
            get
            {
                return mIsDone;
            }
        }

        public override string MaterialName
        { 
            get
            {
                return null;
            }
        }


        public override void EnsureTex(int image_width, int image_height, ref Texture2D tex)
        {
            int tex_width = image_width;
            int tex_height = image_height;
            AFrameConverter.EnsureTex(tex_width, tex_height, mOutputFormat, ref tex);
        }

        public override void Allocate(IFrame from, ref Texture2D tex)
        {
            processing_from = from;
            processing_to = tex;
            EnsureTex(processing_from.Width, processing_from.Height, ref processing_to);
        }

        public override void Convert()
        {
            mIsDone = true;
        }

        public override Texture2D Complete()
        {
            processing_to.LoadRawTextureData(processing_from.Buffer);
            processing_to.Apply();
            Texture2D result = processing_to;

            processing_from.Dispose();
            processing_from = null;
            processing_to = null;
            mIsDone = false;
            return result;
        }

        public override bool IsValidInput(IFrame frame)
        {
            if (frame.Format == FramePixelFormat.ABGR)
                return true;
            return false;
        }

    }

    /// <summary>
    /// Receives I420p and copies it onto the GPU using an R8 texture.
    /// The result requires a special shader to be used as this format is
    /// not directly supported by Unity.
    /// It can also be converted to a regular texture using other converters.
    /// </summary>
    public class FrameConverter_I420p_to_R8 : AFrameConverter
    {

        private IDirectMemoryFrame processing_from;
        private Texture2D processing_to;
        private NativeArray<byte> processing_to_data;
        private int mTextureWidth;
        private int mTextureHeight;
        private int mImageWidth;
        private int mImageHeight;

        private bool mIsDone = false;
        public override bool IsDone {
            get {
                return mIsDone;
            }
        }


        public static readonly TextureFormat mOutputFormat = TextureFormat.R8;
        public override string MaterialName
        {
            get
            {
                return UnityMediaHelper.I420_SINGLE_MAT_NAME;
            }
        }




        public static void EnsureI420pTex(int image_width, int image_height, ref Texture2D tex)
        {
            //top 2 third are luminance. bottom third is chrominance
            int tex_width = image_width;
            int tex_height = image_height + ((image_height + 1) / 2);

            AFrameConverter.EnsureTex(tex_width, tex_height, mOutputFormat, ref tex);
        }
        public override void EnsureTex(int image_width, int image_height, ref Texture2D tex)
        {
            EnsureI420pTex(image_width, image_height, ref tex);
        }


        public override void Allocate(IFrame from, ref Texture2D tex)
        {
            processing_from = from as IDirectMemoryFrame;
            EnsureTex(processing_from.Width, processing_from.Height, ref tex);
            processing_to = tex;
            processing_to_data = tex.GetRawTextureData<byte>();
            mTextureWidth = processing_to.width;
            mTextureHeight = processing_to.height;
            mImageWidth = from.Width;
            mImageHeight = from.Height;
        }

        public override void Convert()
        {
            unsafe
            {
                int ystride = mTextureWidth;
                int ustride = mTextureWidth;
                int vstride = mTextureWidth;

                int uoffset = mTextureWidth * mImageHeight;
                //rounding down here otherwise we shoot past the image and corrupt memory
                int voffset = uoffset + mImageWidth / 2;

                byte* startPtr = (byte*)processing_to_data.GetUnsafePtr<byte>();
                IntPtr y = (IntPtr)(startPtr);
                IntPtr u = (IntPtr)(startPtr + uoffset);
                IntPtr v = (IntPtr)(startPtr + voffset);
                //WARNING: VERY HIGH RISK ACCESS HERE
                //this directly accesses the image within WebRTC's memory
                //if the structure of the memory is different on untested platforms
                //or after updates this will cause crash or memory corruption
                //Copy & convert into a format usable by the shaders
                processing_from.ToBufferI420p(y, ystride, u, ustride, v, vstride);
                mIsDone = true;
            }
        }

        public override Texture2D Complete()
        {
            mIsDone = false;
            processing_to.Apply();
            Texture2D result = processing_to;
            return result;
        }


        public override bool IsValidInput(IFrame frame)
        {
            if (frame.Format == FramePixelFormat.I420p && frame is IDirectMemoryFrame)
                return true;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }

    public struct FrameProcJob : IJob
    {
        public GCHandle dataHandle;

        public void Execute()
        {
            var converter = dataHandle.Target as FrameConverter_I420p_to_R8;
            converter.Convert();
        }
    }

    /// <summary>
    /// Builds on FrameConverter_I420p_to_R8 but converts the results back to 
    /// an RGBA32 texture to make the results easier to use within Unity
    /// </summary>
    public class FrameConverter_I420p_to_RGBA32: AFrameConverter
    {
        private AFrameConverter subConverter;
        
        //tmp texture used to buffer the I420p frame on the GPU
        private Texture2D tempI420p = null;
        //tmp render texture to draw to during I420p to RGBA32 conversion
        private RenderTexture tmpRt = null;
        //material used that handles the I420p to RGBA32 conversion
        private Material mat = null;


        private IDirectMemoryFrame processing_from;
        private Texture2D processing_to;

        /// <summary>
        /// No special shader required to use the output
        /// </summary>
        public override string MaterialName { get { return null; } }

        public override bool IsDone
        {
            get
            {
                return this.subConverter.IsDone;
            }
        }

        public FrameConverter_I420p_to_RGBA32(bool useParallel)
        {
            if (useParallel)
            {
                throw new InvalidOperationException("Parallel support not yet available");
                //subConverter = new FrameConverter_I420p_to_R8_Parallel();
            }
            else
            {
                subConverter = new FrameConverter_I420p_to_R8();
            }
        }


        public override void EnsureTex(int image_width, int image_height, ref Texture2D tex)
        {
            AFrameConverter.EnsureTex(image_width, image_height, TextureFormat.RGBA32, ref tex);
        }

        public void EnsureRenderTex(int texture_width, int texture_height, ref RenderTexture tex)
        {
            var format = RenderTextureFormat.ARGB32;
            if (tex != null && (tex.width != texture_width || tex.height != texture_height || tex.format != format))
            {
                RenderTexture.Destroy(tex);
                tex = null;
            }

            if (tex == null)
            {
                //Setting linear to skip Unity's automatic conversion to linear
                //(actual internal texture format is in gamma and should never be processed)
                tex = new RenderTexture(texture_width, texture_height, 0, format, RenderTextureReadWrite.Linear);
            }
        }

        public override void Allocate(IFrame from, ref Texture2D tex)
        {
            processing_from = from as IDirectMemoryFrame;
            EnsureTex(processing_from.Width, processing_from.Height, ref tex);
            processing_to = tex;
            //create temporary render texture
            EnsureRenderTex(processing_from.Width, processing_from.Height, ref tmpRt);

            //create material
            if (mat == null)
            {
                Material newMaterial = Resources.Load<Material>(UnityMediaHelper.I420_SINGLE_MAT_NAME);
                mat = new Material(newMaterial);

            }

            //create temporary texture to fit the i420p version of the image
            subConverter.Allocate(processing_from, ref tempI420p);
        }

        public override void Convert()
        {
            //move from CPU to GPU
            subConverter.Convert();
        }

        public override Texture2D Complete()
        {
            tempI420p = this.subConverter.Complete();
            //convert on GPU
            Graphics.Blit(tempI420p, tmpRt, mat);
            Graphics.CopyTexture(tmpRt, processing_to);

            Texture2D result = processing_to;
            processing_to = null;
            return result;
        }


        public override bool IsValidInput(IFrame frame)
        {
            return subConverter.IsValidInput(frame);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(subConverter != null)
                {
                    subConverter.Dispose();
                    subConverter = null;
                }
                if(tempI420p != null)
                {
                    Texture2D.Destroy(tempI420p);
                    tempI420p = null;
                }
                if (tmpRt != null)
                {
                    Texture2D.Destroy(tmpRt);
                    tmpRt = null;
                }
                if (mat != null)
                {
                    Texture2D.Destroy(mat);
                    mat = null;
                }
            }
            base.Dispose(disposing);
        }

    }

    /// <summary>
    /// Native frame converter for WebGL.
    /// This is essentially a dummy for now.
    /// 
    /// The WebGL specific plugin itself handles the native textures. 
    /// We just unpack them during the allocate method and return
    /// the texture in Complete.
    /// 
    /// </summary>
    public class FrameConverter_WebGL_Native : AFrameConverter
    {
        private bool mDone = false;
        public override bool IsDone { get { return mDone; } }

        public override string MaterialName { get { return null; } }

        private Texture2D mTexture;

        public override void Allocate(IFrame from, ref Texture2D tex)
        {
            TextureFrame tfr = from as TextureFrame;
            var newTexture = tfr.TakeOwnership();
            //Native WebGL plugin can not reuse existing textures. 
            //We destroy any textures passed in for reuse
            if(tex != null && tex != newTexture){
                Texture2D.Destroy(tex);
                tex = null;
            }
            tex = newTexture;
            mTexture = newTexture;
        }


        public override void Convert()
        {
            mDone = true;
        }
        public override Texture2D Complete()
        {
            mDone = false;
            var res = mTexture;
            mTexture = null;
            return res;
        }

        public override bool IsValidInput(IFrame frame)
        {
            if(frame is TextureFrame)
            {
                return true;
            }
            return false;
        }

        public override void EnsureTex(int image_width, int image_height, ref Texture2D tex)
        {

        }
    }

    /// <summary>
    /// Contains any additional information about a frame. 
    /// </summary>
    public class FrameMetaData
    {
        /// <summary>
        /// Height of the original IFrame. 
        /// NOTE: In some cases this can differ from the actual pixel height of the associated 
        /// Texture2D. If the image is stored as I420p Color is stored separately below the grayscale image
        /// and thus the Texture.height is 50% bigger than Height.
        /// This value will indicate the true resolution of the image
        /// </summary>
        public int Height;
        /// <summary>
        /// Width of the original IFrame
        /// </summary>
        public int Width;

        /// <summary>
        /// Rotation of the image. This indicates a rotation that must be performed by the
        /// UI before showing the image to the user. 
        /// e.g. an Android device that is rotated to Portrait mode might continue recording in 
        /// Landscape mode. This value is then set to 90 degrees to indicate it must be rotated via the
        /// UI before showing the content to the user. 
        /// </summary>
        public int Rotation;

        /// <summary>
        /// If true the top row of the image will be at the start of the buffer.
        /// If this value is true (default) the image needs to be flipped vertically as Unity
        /// expects the top row to be at the end of the buffer thus Unity will read the image
        /// up-side-down. To undo this you can set the object scaleY to -1. 
        /// </summary>
        public bool IsTopRowFirst;

        /// <summary>
        /// Format of the original IFrame
        /// </summary>
        public FramePixelFormat SourceFormat;

        /// <summary>
        /// ConnectionId indicating where the image was received from. 
        /// This is ConnectionId.INVALID for local frames.
        /// </summary>
        public ConnectionId ConnectionId;

        /// <summary>
        /// true means this image was received via Network. 
        /// false indicates the image is from a local video device
        /// </summary>
        public bool IsRemote
        {
            get
            {
                return ConnectionId != ConnectionId.INVALID;
            }
        }

        /// <summary>
        /// Creates a copy of all meta data associated with the
        /// FrameUpdateEventArgs and IFrame.
        /// </summary>
        /// <param name="args">Event generated by ICall to use as source for this
        /// MetaData instance. </param>
        public FrameMetaData(FrameUpdateEventArgs args)
        {
            IFrame frame = args.Frame;
            Height = frame.Height;
            Width = frame.Width;
            Rotation = frame.Rotation;
            IsTopRowFirst = frame.IsTopRowFirst;
            SourceFormat = frame.Format;
            ConnectionId = args.ConnectionId;
        }
    }
}