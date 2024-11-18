using Byn.Awrtc;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Represents a device that depends on AudioInput. Used to ensure they
    /// are destroyed on shutdown.
    /// </summary>
    public interface IAudioInputDependency
    {
        /// <summary>
        /// System is initializing and delivering the AudioInput instance.
        /// In this method AddDevice should be called.
        /// Method is called only once.
        /// </summary>
        /// <param name="input">AudioInput instance to use</param>
        public void SetupDevice(WebRtcCSharp.AudioInput input);
        /// <summary>
        /// Call RemoveDevice and stop using AudioInput which will be deleted after this call
        /// </summary>
        public void DestroyDevice();
    }
    /// <summary>
    /// This interface is currently used to add a few features on the Unity side that are missing from
    /// the AudioInput API. AudioInputCallApp will trigger OnAccessStart and OnAccessStop to notify 
    /// a device that it must now start / stop recording.
    /// </summary>
    public interface IAudioInputDevice : IAudioInputDependency
    {
        public string DeviceName { get; }

        public void OnAccessStart();
        public void OnAccessStop();
    }


    /// <summary>
    /// Base class for C# side virtual audio devices in combination with AudioInputCallApp.
    /// Use this to create other classes that act like a recording device and send audio to ICall/IMediaNetwork
    /// by setting their name to MediaConfig.AudioDeviceName. 
    /// 
    /// The tasks of this are:
    /// * It encapsulates the less stable AudioInput API to reduce the risk of crashes and allow this API to change in the future
    /// * It keeps track of the DeviceName
    /// * Keeps track of Initialized (device becomes accessible) and Accessed states (device is actively being used)
    /// 
    /// See sub classes for details.
    /// 
    /// </summary>
    public abstract class VirtualAudioInput : MonoBehaviour, IAudioInputDevice
    {
        /// <summary>
        /// Any generators should aim to generate 10ms chunks at once as this is the only
        /// size WebRTC can directly process. Anything above or below 10ms leads to buffering.
        /// </summary>
        public static readonly int IDEAL_CHUNK_SIZE_MS = 10;

        public string _DeviceName = "unity_audio";
        public string DeviceName { get { return _DeviceName; } }

        private bool mIsInitialized = false;
        /// <summary>
        /// True - the device is ready to be used and advertised in as an available audio device.
        /// False - The device is either not connected to an AudioInputApp or
        /// </summary>
        public bool IsInitialized
        {
            get { return mIsInitialized; }
        }

        protected int mAccessCounter = 0;
        public bool IsAccessed { 
            get { return mAccessCounter > 0; }
        }


        private WebRtcCSharp.AudioInput mInput;


        private bool mLargeBufferWarning = false;


        protected virtual void Awake()
        {

        }

        /// <summary>
        /// Method called by AudioInputCallApp
        /// after making sure the factory is initialized
        /// </summary>
        /// <param name="deviceName"></param>
        public virtual void SetupDevice(WebRtcCSharp.AudioInput input)
        {
            mInput = input;
            mIsInitialized = true;
            Debug.Log("Adding virtual audio device " + this.GetType().Name + " as "+ _DeviceName);
            mInput.AddDevice(_DeviceName);

        }

        /// <summary>
        /// Makes sure to remove the virtual device if the GameObject is removed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            TryDestroy();
        }
        /// <summary>
        /// Called when our factory is destroyed (e.g. game exits)
        /// </summary>
        public void DestroyDevice()
        {
            TryDestroy();
        }

        private void TryDestroy()
        {
            if (this.IsInitialized)
            {
                Debug.Log("Removing virtual audio device " + _DeviceName);
                mInput.RemoveDevice(_DeviceName);
                this.mIsInitialized = false;
                mInput = null;
                AudioInputExample.Instance.OnDependencyDestroyed(this);
            }
        }

        /// <summary>
        /// Adds audio frames to the buffer. 
        /// 
        /// Ideally, send 10ms (IDEAL_CHUNK_SIZE_MS) of audio at once. If less than 10ms
        /// is sent it will be buffered until 10ms of audio is available. 
        /// </summary>
        /// <param name="audio">
        /// Samples in 16bit arrays with channels interleaved. 
        /// </param>
        /// <returns>-1 for error. Otherwise it indicates the sample that are currently buffered. </returns>
        protected int AddFrameData(short[] audio, int sampleRate, int channels)
        {
            
            if (mIsInitialized == false)
            {
                Debug.LogError("mIsInitialized called with uninitialized device. Frames are dropped.");
                return -1;
            }
            if (IsAccessed == false)
            {
                //If this warning is triggered check IsAccessed before calling this method.
                //This is to ensure that we do not generate, convert and process audio just for it
                //to be ignored by WebRTC later on.
                Debug.LogWarning("AddFrameData called with inactive device (IsAccessed == false). Frames are dropped.");
                return -1;
            }
            //We have to be very careful here. Any incorrect input can easily cause
            //memory corruption or the entire app to crash!!!
            int buffer = mInput.UpdateFrame(_DeviceName, sampleRate, channels, audio, audio.Length);

            //buffer < 0 is an error that should never happen unless there is a programming error
            if (buffer < 0)
            {
                //Typically means the input is invalid e.g. the device doesn't exist or sampleRate is 0.
                Debug.LogError("AddFrameData returned value < 0. Invalid input.");
            }
            //We currently rely on both WebRTC and Unity to generate and consume audio data following the real time
            //clock. If Unity is too fast or WebRTC is too slow we will run into problems. One solution might be to
            //skip some samples.
            if (mLargeBufferWarning == false && buffer > (sampleRate * channels) * 0.1)
            {
                //Typically means we try to work with a virtual device that has been deleted
                Debug.LogWarning($"AddFrameData buffer unusually large at ${buffer} bytes with ${sampleRate} and ${channels}");
                mLargeBufferWarning = true;
            }

            return buffer;
        }

        public void OnAccessStart()
        {
            Debug.Log("AccessStart for " + this.gameObject.name);
            if(mIsInitialized == false)
            {
                Debug.LogError("Attempted to access a microphone not yet initialized.");
                this.enabled = false;
                return;
            }
            mAccessCounter++;
            if(mAccessCounter == 1)
                StartRecording();
        }
        public void OnAccessStop()
        {
            Debug.Log("OnAccessStop for " + this.gameObject.name);
            mAccessCounter--;
            if (mAccessCounter == 0)
                StopRecording();
        }

        /// <summary>
        /// Override in subclass if setup is required when recording starts. 
        /// If this is not needed poll IsAccessed.
        /// </summary>
        protected virtual void StartRecording() { }
        /// <summary>
        /// Override in subclass if shutdown is needed when recording stops.
        /// If this is not needed poll IsAccessed.
        /// </summary>
        protected virtual void StopRecording() { }




    }
}