/* 
 * Copyright (C) 2022 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using UnityEngine;

namespace Byn.Awrtc.Unity
{
    /// <summary>
    /// Interface used for Frames that contain a Texture2D.
    /// See TakeOwnership for how to use this correctly.
    /// </summary>
    public interface ITextureFrame : IFrame
    {
        //Removed to reduce the risk of memory leaks
        //Use TakeOwnership();
        //Texture2D Texture
        //{
        //    get;
        //}

        /// <summary>
        /// Call once to return the internal Texture2D object.
        /// After calling the callee is responsible for using
        /// Object.Destroy to cleanup the texture after use. 
        /// If TakeOwnership is not called the texture is destroyed on
        /// Dispose()
        /// </summary>
        /// <returns>
        /// Returns the internal Texture2D object
        /// </returns>
        Texture2D TakeOwnership();
    }
    /// <summary>
    /// Container for Texture2D + meta data. It implements 
    /// IFrame to allow handling it like the buffered frames. 
    /// 
    /// Note this frame needs special handling and Buffer returns null at the moment to avoid
    /// inefficient code.
    /// </summary>
    public class TextureFrame : ITextureFrame
    {
        private Texture2D mTexture;

        public byte[] Buffer
        {
            get
            {
                return null;
            }
        }

        public bool Buffered
        {
            get
            {
                return false;
            }
        }

        private int mWidth;
        public int Width
        {
            get
            {
                return mWidth;
            }
        }

        private int mHeight;
        public int Height
        {
            get
            {
                return mHeight;
            }
        }

        public int Rotation
        {
            get
            {
                return 0;
            }
        }

        public bool IsTopRowFirst
        {
            get
            {
                return true;
            }
        }

        public FramePixelFormat Format
        {
            get
            {
                return FramePixelFormat.Native;
            }
        }

        public TextureFrame(Texture2D tex)
        {
            this.mTexture = tex;
            this.mWidth = mTexture.width;
            this.mHeight = mTexture.height;
        }

        public Texture2D TakeOwnership()
        {
            var res = mTexture;
            mTexture = null;
            return res;
        }

        public void Dispose()
        {
            if (this.mTexture != null)
            {
                UnityEngine.Object.Destroy(mTexture);
                mTexture = null;
            }
        }
    }
}