/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using Byn.Awrtc;
using System;
using UnityEngine;

namespace Byn.Awrtc.Unity
{
    /// <summary>
    /// See IVideoInput for documentation. 
    /// Use IVideoInput whenever possible. VideoInput might get split up into several platform specific classes.
    /// </summary>
    public class UnityVideoInput : IVideoInput
    {
        private TextureFormat mFormat;
        /// <summary>
        /// Returns the Unity Texture2D format that is supported as input for UpdateFrame. 
        /// </summary>
        public TextureFormat Format
        {
            get
            {
                return mFormat;
            }
        }



        public VideoInputFormat InputFormat
        {
            get
            {
                return mInternal.InputFormat;
            }
        }

        private IVideoInput mInternal;
        public IVideoInput Internal
        {
            get
            {
                return mInternal;
            }
        }

        public UnityVideoInput(IVideoInput platformVideoInput)
        {
            mInternal = platformVideoInput;
            InitFormat();
        }

        private void InitFormat()
        {
            //check if the Unity texture formats & binary image formats of the dll's align
            //If this error ever triggers then the Unity C# code is likely out the plugin dll's
            //Try to delete WebRtcVideoChat and reimport a clean version
            string err = "The underlaying platform requests an unsupported format. "
                    + "This indicates this version of UnityVideoInput is not up-to-date! "
                    + "Format requested:" + mInternal.InputFormat;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (mInternal.InputFormat != VideoInputFormat.ABGR)
                throw new FormatException(err);
            mFormat = TextureFormat.RGBA32;
#else

            if (mInternal.InputFormat != VideoInputFormat.BGRA)
                throw new FormatException(err);
            mFormat = TextureFormat.ARGB32;
#endif
        }



        /// <summary>
        /// Updates the video frame using a Texture2D. It must have the format 
        /// returned by the Format property!
        /// Method will trigger a FormatException if the input format is not supported.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="texture"></param>
        /// <param name="rotation"></param>
        /// <param name="firstRowIsBottom"></param>
        /// <returns>True if the frame could be updated correctly.</returns>
        public bool UpdateFrame(string name, Texture2D texture, int rotation, bool firstRowIsBottom)
        {
            if (texture.format != Format)
                throw new FormatException("Only " + Format + " supported but found " + texture.format + " . Use platform specific VideoInput for any customization. ");
            var dataBuffer = texture.GetRawTextureData();
            return UpdateFrame(name, dataBuffer, texture.width, texture.height, rotation, firstRowIsBottom);
        }

        #region wrapper methods

        public void AddDevice(string name, int width, int height, int fps)
        {
            mInternal.AddDevice(name, width, height, fps);
        }

        public void RemoveDevice(string name)
        {
            mInternal.RemoveDevice(name);
        }
        public bool UpdateFrame(string name, byte[] dataBuffer, int width, int height, int rotation, bool firstRowIsBottom)
        {
            return mInternal.UpdateFrame(name, dataBuffer, width, height, rotation, firstRowIsBottom);
        }
        public bool UpdateFrame(string name, IntPtr dataPtr, int length, int width, int height, int rotation, bool firstRowIsBottom)
        {
            return mInternal.UpdateFrame(name, dataPtr, length, width, height, rotation, firstRowIsBottom);
        }
        #endregion

    }
}
