/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

namespace Byn.Awrtc.Unity
{
    public class UnityMediaHelper
    {
        /// <summary>
        /// Material used to show i420 image via Unitys RawImage
        /// </summary>
        public static readonly string I420_SINGLE_MAT_NAME = "I420_one_buffer";

        /// <summary>
        /// Updates a texture with a new IFrame. 
        /// Only ABGR (RGBA32 in unity) frames are properly supported at the moment.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="tex"></param>
        /// <returns></returns>
        public static bool UpdateTexture(IFrame frame, ref Texture2D tex)
        {
            var format = frame.Format;
            if (frame.Format == FramePixelFormat.ABGR)
            {
                bool newTextureCreated = false;
                //texture exists but has the wrong height /width? -> destroy it and set the value to null
                if (tex != null && (tex.width != frame.Width || tex.height != frame.Height))
                {
                    Texture2D.Destroy(tex);
                    tex = null;
                }
                //no texture? create a new one first
                if (tex == null)
                {
                    newTextureCreated = true;
                    Debug.Log("Creating new texture with resolution " + frame.Width + "x" + frame.Height + " Format:" + format);

                    //so far only ABGR is really supported. this will change later
                    if (format == FramePixelFormat.ABGR)
                    {
                        tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
                    }
                    else
                    {
                        Debug.LogWarning("YUY2 texture is set. This is only for testing");
                        tex = new Texture2D(frame.Width, frame.Height, TextureFormat.YUY2, false);
                    }
                    tex.wrapMode = TextureWrapMode.Clamp;
                }
                //copy image data into the texture and apply
                //Watch out the RawImage has the top pixels in the top row but
                //unity has the top pixels in the bottom row. Result is an image that is
                //flipped. Fixing this here would waste a lot of CPU power thus
                //the UI will simply set scale.Y of the UI element to -1 to reverse this.
                tex.LoadRawTextureData(frame.Buffer);
                tex.Apply();
                return newTextureCreated;
            }
            else if (frame.Format == FramePixelFormat.I420p && frame is IDirectMemoryFrame)
            {
                //Watch out this conversion is only possible on native platforms
                //and some might still crash later if their internal doesn't use the correct internal format
                var dframe = frame as IDirectMemoryFrame;

                bool newTextureCreated = EnsureTex(frame, TextureFormat.R8, ref tex);

                
                NativeArray<byte> data = tex.GetRawTextureData<byte>();
                if(data == null || data.IsCreated == false)
                {
                    Debug.LogWarning("GetRawTextureData failed");
                    return false;
                }
                unsafe
                {
                    int ystride = tex.width;
                    int ustride = tex.width;
                    int vstride = tex.width;

                    int uoffset = tex.width * dframe.Height;
                    //rounding down here otherwise we shoot past the image and corrupt memory
                    int voffset = uoffset + dframe.Width / 2;
                    byte* startPtr = (byte*)data.GetUnsafePtr<byte>();
                    IntPtr y = (IntPtr)(startPtr);
                    IntPtr u = (IntPtr)(startPtr + uoffset);
                    IntPtr v = (IntPtr)(startPtr + voffset);
                    //WARNING: VERY HIGH RISK ACCESS HERE
                    //this directly accesses the image within WebRTC's memory
                    //if the structure of the memory is different on untested platforms
                    //or after updates this will cause a crash or memory corruption
                    //Copy & convert into a format usable by the shaders
                    dframe.ToBufferI420p(y, ystride, u, ustride, v, vstride);
                }

                dframe.Dispose();
                tex.Apply();
                return newTextureCreated;
            }
            else if (frame.Format == FramePixelFormat.Native)
            {
                var dframe = frame as TextureFrame;
                var newTexture = dframe.TakeOwnership();
                //if the system created a new texture destroy the old
                if (tex != null && tex != newTexture)
                {
                    //Debug.Log("Destroying old texture " + tex.width + "x" + tex.height + ". New tex " + newTexture.width + "x" + newTexture.height);
                    Texture2D.Destroy(tex);
                    tex = null;
                }
                tex = newTexture;
                //Debug.Log("Texture id out: " + (int)tex.GetNativeTexturePtr());
                dframe.Dispose();
                //tex.Apply();
                return true;
            }
            else
            {
                Debug.LogError("Format not supported");
                return false;
            }
        }

        public static void CalcTextureResolution(IFrame frame, out int width, out int height)
        {
            width = frame.Width;
            height = frame.Height;
            if (frame is IDirectMemoryFrame)
            {
                height = frame.Height + ((frame.Height + 1) / 2);
            }
        }
        public static bool NeedsRecreate(IFrame frame, Texture2D tex)
        {
            if (tex == null)
                return true;
            int width;
            int height;
            CalcTextureResolution(frame, out width, out height);
            if ((tex.width != width || tex.height != height))
                return true;
            return false;
        }

        /// <summary>
        /// Helper to ensure the texture has a fixed size and format.
        /// If not it will be created / destroyed and recreated
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texFormat"></param>
        /// <param name="tex"></param>
        /// <returns>True if a new texture was created.
        /// Meaning external references need to be refreshed!
        /// </returns>
        private static bool EnsureTex(IFrame frame, TextureFormat texFormat, ref Texture2D tex)
        {
            if (NeedsRecreate(frame, tex))
            {
                Texture2D.Destroy(tex);
                tex = null;
            }
            if (tex == null)
            {
                int width;
                int height;
                CalcTextureResolution(frame, out width, out height);
                tex = new Texture2D(width, height, texFormat, false);
                return true;
            }
            else
            {
                return false;
            }
        }
        



        /// <summary>
        /// Helper to update a RawImage based on IFrame.
        /// 
        /// Watch out this method might change the material and textures 
        /// used for the RawImage.
        /// 
        /// This method can be used in the future to support i420p using
        /// a specific shader / material / texture combination.
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static bool UpdateRawImage(RawImage target, IFrame frame)
        {
            if (target == null || frame == null)
                return false;
            bool textureCreated;

            if (frame.Format == FramePixelFormat.I420p
                && frame is IDirectMemoryFrame)
            {

                string matname = I420_SINGLE_MAT_NAME;
                if (matname == null)
                {
                    if (target.material != target.defaultMaterial)
                    {
                        target.material = target.defaultMaterial;
                    }
                }
                else if (target.material.name != matname)
                {
                    Material mat = Resources.Load<Material>(matname);
                    target.material = new Material(mat);
                }

                Texture2D mainTex = target.texture as Texture2D;
                //remove for now to avoid destroying a used texture
                if (NeedsRecreate(frame, mainTex))
                {
                    target.texture = null;
                }

                //update existing textures or create new ones
                textureCreated = UnityMediaHelper.UpdateTexture(frame, ref mainTex);
                if (textureCreated)
                {
                    //new textures where creates (e.g. due to resolution change)
                    //update the UI
                    target.texture = mainTex;
                }
                frame.Dispose();
                frame = null;
            }
            else
            {
                if (target.material != target.defaultMaterial)
                {
                    target.material = target.defaultMaterial;
                }
                Texture2D mainTex = target.texture as Texture2D;
                textureCreated = UnityMediaHelper.UpdateTexture(frame, ref mainTex);
                target.texture = mainTex;
            }
            return textureCreated;
        }

        public static bool UpdateRawImageTransform(RawImage target, IFrame frame, bool mirror)
        {
            bool textureCreated = UpdateRawImage(target, frame);
            float mirrorVal;
            float rotFactor;
            if (mirror)
            {
                mirrorVal = -1;
                rotFactor = 1;
            }
            else
            {
                mirrorVal = 1;
                rotFactor = -1;
            }


            float upSideDown = 1;
            if (frame.IsTopRowFirst)
            {
                upSideDown = -1;
            }

            target.transform.localScale = new Vector3(mirrorVal, upSideDown, 1);
            target.transform.localRotation = Quaternion.Euler(0, 0, frame.Rotation * rotFactor);
            return textureCreated;
        }




        //do not use. Note all platforms return an image in the expected format anymore
        /*
        public static bool UpdateTextureThreeBuffers(IDirectMemoryFrame frame, ref Texture2D yplane, ref Texture2D uplane, ref Texture2D vplane)
        {

            if (frame.Format == FramePixelFormat.I420p)
            {
                var dframe = frame as IDirectMemoryFrame;
                int width = frame.Width;
                int height = frame.Height;
                int hwidth = frame.Width / 2;
                int hheight = (frame.Height + 1) / 2;
                TextureFormat texFormat = TextureFormat.R8;
                bool newTextureCreated = EnsureTexRaw(width, height, texFormat, ref yplane);
                newTextureCreated |= EnsureTexRaw(hwidth, hheight, texFormat, ref uplane);
                newTextureCreated |= EnsureTexRaw(hwidth, hheight, texFormat, ref vplane);

                IntPtr ystart = dframe.GetIntPtr();
                long ylength = width * height;
                IntPtr ustart = new IntPtr(ystart.ToInt64() + ylength);
                long ulength = (hwidth * hheight);
                IntPtr vstart = new IntPtr(ustart.ToInt64() + ulength);

                yplane.LoadRawTextureData(ystart, (int)ylength);
                uplane.LoadRawTextureData(ustart, (int)ulength);
                vplane.LoadRawTextureData(vstart, (int)ulength);
                yplane.Apply();
                uplane.Apply();
                vplane.Apply();
                return newTextureCreated;
            }
            else
            {
                Debug.LogError("Format not supported");
                return false;
            }
        }
        
        private static bool EnsureTexRaw(int width, int height, TextureFormat texFormat, ref Texture2D tex)
        {
            if (tex != null)
            {
                Texture2D.Destroy(tex);
                tex = null;
            }
            if (tex == null)
            {
                tex = new Texture2D(width, height, texFormat, false);
                return true;
            }
            else
            {
                return false;
            }
        }
        */
    }
}
