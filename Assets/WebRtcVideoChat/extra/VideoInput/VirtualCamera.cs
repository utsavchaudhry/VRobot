/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
#define ALLOW_UNSAFE
using Byn.Awrtc.Unity;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Example for use of VideoInput class. 
    /// 
    /// This script will create a virtual video camera which then
    /// can be selected like a webcam via the CallApp.
    /// 
    /// It will obtain its image via a Unity Camera object and then store
    /// it in a native buffer. The image can be used later via the
    /// ICall or IMediaNetwork interface by setting
    /// MediaConfig.VideoDeviceName to the value of VirtualCamera._DeviceName.
    /// 
    /// Test the performance of this system first before using it. It might
    /// be too slow for many systems if used with high resolution images.
    /// 
    /// Note this example was not written for HDR / Linear textures.
    /// For this a few extra steps are needed:
    /// 
    /// 1. Allocate the render texture as ARGBHalf:
    /// RenderTexture mRtBuffer = new RenderTexture(_Width, _Height, 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
    /// 
    /// 2. Add a texture as target for the conversion (Linear -> RGB)
    /// RenderTexture convertTex = RenderTexture.GetTemporary(_Width, _Height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
    /// 
    /// 3. Before read pixels or AsyncReadback convert to RGB and later read from convertTex
    /// Graphics.Blit(mRtBuffer, convertTex);
    /// 
    /// 
    /// </summary>
    public class VirtualCamera : MonoBehaviour
    {
        public Camera _Camera;
        private float mLastSample;

        private Texture2D mTexture;
        private RenderTexture mRtBuffer = null;

        /// <summary>
        /// Can be used to output the image sent for testing
        /// </summary>
        public RawImage _DebugTarget = null;

        /// <summary>
        /// Name used to access it later via MediaConfig
        /// </summary>
        public string _DeviceName = "VirtualCamera1";

        /// <summary>
        /// FPS the virtual device is suppose to have.
        /// (This isn't really used yet except to filter
        /// out this device if MediaConfig requests specific FPS)
        /// </summary>
        public int _Fps = 60;


        /// <summary>
        /// Width the output is suppose to have
        /// </summary>
        public int _Width = 1280;
        /// <summary>
        /// Height the output is suppose to have
        /// </summary>
        public int _Height = 720;

        /// <summary>
        /// Device name used by this instance.
        /// </summary>
        private string mUsedDeviceName;


        /// <summary>
        /// Interface for video device input.
        /// </summary>
        private UnityVideoInput mVideoInput;
        
        
        /// <summary>
        /// If unity supports it on the specific GfxDevice then it will be set to true. 
        /// </summary>
        private static bool mUseAsyncReadback = false;


        private void Awake()
        {
#if ALLOW_UNSAFE
            //Remove this line to deactivate it permanently. Some platforms & device combinations do
            //seem to support it yet fail sometimes randomly
#if UNITY_WEBGL
            Debug.LogWarning("Turned off AsyncGPUReadback for WebGL. This appears to trigger a memory leak when OnAsyncGPUReadback is called.");
            mUseAsyncReadback = false;
#else
            mUseAsyncReadback = SystemInfo.supportsAsyncGPUReadback;
#endif
            if (mUseAsyncReadback)
            {
                Debug.LogWarning("UseAsyncReadback is active. This increases the FPS but can cause errors on some devices.");
            }
            else
            {
                Debug.Log("UseAsyncReadback is disabled. This might reduce overall FPS.");
            }
#else
            Debug.Log("UseAsyncReadback and unsafe access is disabled. This will heavily reduce FPS.");
#endif

            mUsedDeviceName = _DeviceName;

        }


        void Start()
        {
            UnityCallFactory.EnsureInit(() =>
            {
                OnAwrtcInit();
            });
        }

        private void OnAwrtcInit()
        {
            mVideoInput = UnityCallFactory.Instance.VideoInput;
            if(mVideoInput == null)
            {
                Debug.LogError("VideoInput returned null. This platform might not support video input or an error stopped the init process.");
                return;
            }

            if (CheckResolution() == false)
            {
                this.enabled = false;
                return;
            }
            mVideoInput.AddDevice(mUsedDeviceName, _Width, _Height, _Fps);
        }

        private bool CheckResolution()
        {
            if (_Height < 16 || _Width < 16)
            {
                //There is no documented limit but it isn't quite clear how well all the different codecs,
                //platform and hardware might react to unusual resolutions
                Debug.LogWarning("Resolution too low " + _Width + "x" + _Height);
                return false;
            }
            return true;
        }

        private bool AreBuffersValid()
        {
            return mRtBuffer != null && mTexture != null && mRtBuffer.width == _Width && mRtBuffer.height == _Height;
        }
        private void RecreateBuffers()
        {
            if (mRtBuffer != null)
                Destroy(mRtBuffer);
            mRtBuffer = new RenderTexture(_Width, _Height, 16, RenderTextureFormat.ARGB32);
            mRtBuffer.wrapMode = TextureWrapMode.Repeat;

            if (mTexture != null)
                Destroy(mTexture);
            mTexture = new Texture2D(_Width, _Height, mVideoInput.Format, false);
        }
        private void OnDestroy()
        {
            Destroy(mRtBuffer);
            Destroy(mTexture);

            if (mVideoInput != null)
                mVideoInput.RemoveDevice(mUsedDeviceName);

        }


        private void UpdateFrame()
        {
            //read frame data written to mTexture last time
#if ALLOW_UNSAFE

            //this avoids one copy compared to unity 2017 method
            var data = mTexture.GetRawTextureData<byte>();
            unsafe
            {
                var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
                //update the internal WebRTC device 
                mVideoInput.UpdateFrame(mUsedDeviceName, ptr, data.Length, mTexture.width, mTexture.height, 0, true);
            }
#else


            mVideoInput.UpdateFrame(mUsedDeviceName, mTexture, 0, true);

#endif
        }

        void OnAsyncGPUReadback(AsyncGPUReadbackRequest req)
        {
            //This doesn't seem to work well in WebGL apps. It appears to fail randomly.
            if (req.hasError)
            {
                //if you get this error set ASYNC_READBACK to false
                Debug.LogError("AsyncGPUReadbackRequest has returned an error. Skipping frame.");
                return;
            }

            var data = req.GetData<byte>();
            unsafe
            {
                var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
                mVideoInput.UpdateFrame(mUsedDeviceName, ptr, data.Length, req.width, req.height, 0, true);

            }

        }


        void Update()
        {
            if (mVideoInput == null)
                return; //not yet initialized

            if(CheckResolution() == false)
            {
                return;
            }
            if (AreBuffersValid() == false)
            {
                RecreateBuffers();
            }
            //ensure correct fps
            float deltaSample = 1.0f / _Fps;
            mLastSample += Time.deltaTime;
            if (mLastSample >= deltaSample)
            {
                mLastSample -= deltaSample;

                if (mUseAsyncReadback == false)
                {
                    //access frame data we read during a previous frame.
                    UpdateFrame();
                }

                //backup the current configuration to restore it later
                var oldTargetTexture = _Camera.targetTexture;
                var oldActiveTexture = RenderTexture.active;

                //Set the buffer as target and render the view of the camera into it
                _Camera.targetTexture = mRtBuffer;
                _Camera.Render();
                if (mUseAsyncReadback)
                {
                    //we read mTexture on the next loop giving the GPU time to sync
                    AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(mRtBuffer, 0, mVideoInput.Format, OnAsyncGPUReadback);
                }
                else
                {
                    //without async readback we still try to add some delay between the access on the GPU
                    //and access on CPU. Move the pixels on the GPU to the pixels at the end of a frame
                    //and then access it on the next 
                    RenderTexture.active = mRtBuffer;
                    mTexture.ReadPixels(new Rect(0, 0, mRtBuffer.width, mRtBuffer.height), 0, 0, false);
                    mTexture.Apply();
                }



                //reset the camera/active render texture  in case it is still used for other purposes
                _Camera.targetTexture = oldTargetTexture;
                RenderTexture.active = oldActiveTexture;

                //update debug output if available
                if (_DebugTarget != null)
                {

                    if (mUseAsyncReadback)
                    {
                        _DebugTarget.texture = mRtBuffer;
                    }
                    else
                    {
                        _DebugTarget.texture = mTexture;
                    }
                }
            }
        }

    }
}
