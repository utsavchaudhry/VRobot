using Byn.Unity.Examples;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WebRtcCSharp;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// This will use Unity's Microphone class to generate an AudioClip and plays it via a
    /// newly created AudioSource attached to the same GameObject.
    /// The base class UnityAudioInput will then automatically send this audio.
    /// </summary>
    public class MicrophoneAudioInput : UnityAudioInput
    {
        public static readonly bool DEBUG = true;

        /// <summary>
        /// Name of the microphone to record for. 
        /// Can be set via UnityEditor for fixed setup or 
        /// programmatically via a script before using it.
        /// </summary>
        public string _MicrophoneName;

        private AudioSource mAudioSource;
        public AudioSource AudioSource
        {
            get { return mAudioSource; }
        }

        private bool mIsRecording = false;
        /// <summary>
        /// True if the microphone is actively recording right now
        /// </summary>
        public bool IsRecording { get { return mIsRecording; } }


        private float mDelay = 0;
        /// <summary>
        /// Returns the delay between Microphone time and AudioSource time in seconds. 
        /// Lower value reduces the risk of a buffer underrun. 
        /// Higher value increases desync and latency. 
        /// </summary>
        public float Delay
        {
            get { return mDelay; }
        }

        /// <summary>
        /// True will automatically adjust the AudioSource.time value to replay / send audio 
        /// within a certain delay after the Microphone class recorded the audio.
        /// This is used as a workaround for cracking issues and excessive audio delay caused by
        /// Microphone class and AudioSource going out of sync. 
        /// </summary>
        public bool MonitorDelay = true;

        /// <summary>
        /// Min. adjustment we make to the time of the AudioSource to keep within a certain delay.
        /// </summary>
        public float MinBufferAdjustment = 0.01f;

        /// <summary>
        /// Min time we allow to buffer ahead before we process the audio. 
        /// </summary>
        public float MinBufferTime = 0.01f;

        /// <summary>
        /// Max time we allow to buffer before adjust
        /// </summary>
        public float MaxBufferTime = 0.05f;



        protected override void Awake()
        {
            base.Awake();
            //always mute playback
            _MutePlayback = true;
            if (DEBUG)
            {
                foreach (var v in Microphone.devices)
                    Debug.Log(v);

                this.gameObject.AddComponent<MicrophoneDebugHelper>();
            }
        }

        protected override void StartRecording()
        {
            //warning if no microphone found.
            if(Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone found. Won't be able to access hardware microphone " + _MicrophoneName);
                return;
            }
            //if no microphone set default to the first one
            if(string.IsNullOrEmpty(_MicrophoneName))
                _MicrophoneName = Microphone.devices[0];

            //warning if configured microphone does not exist
            if (Microphone.devices.Contains(_MicrophoneName) == false)
            {
                Debug.LogWarning("Unable to to find microphone " + _MicrophoneName);
                return;
            }

            //We currently assume our SampleRate used for output by Unity can be used by the microphone
            //(or unity converts it for us). It isn't clear if this is always the case though.
            if (DEBUG)
                Debug.Log("Starting access to hardware microphone " + _MicrophoneName + "with sample rate" + SampleRate);
            var m_clipInput = Microphone.Start(_MicrophoneName, true, 1, SampleRate);
            if(m_clipInput == null)
            {
                Debug.LogError("Failed to start recording using " + _MicrophoneName + " and sample rate: " + SampleRate);
                return;
            }
            mIsRecording = true;

            //this blocks until the microphone actually started playback
            //without we risk garbled audio because we try to playback audio data the microphone hasn't written yet.
            while (!(Microphone.GetPosition(_MicrophoneName) > 0)) { }

            //Setup an audio source to send the microphone audio to. This is then captured and processed
            //by the base class
            mAudioSource = this.gameObject.AddComponent<AudioSource>();
            mAudioSource.enabled = true;
            mAudioSource.loop = true;
            mAudioSource.clip = m_clipInput;
            mAudioSource.Play();
        }

        /// <summary>
        /// Calculating the delay between microphone time and time of our audio source.
        /// Both work like ring buffers of the length 1s (1x SampleRate).
        /// </summary>
        /// <param name="micTime">Microphone time. position divided by SampleRate </param>
        /// <param name="sourceTime">.time property on the AudioSource </param>
        /// <returns>Delay in seconds</returns>
        private float CalcDelay(float micTime, float sourceTime)
        {
            //ring buffer
            if (micTime < sourceTime)
                micTime += 1;
            float difference = micTime - sourceTime;
            return difference;
        }

        /// <summary>
        /// This stops recording and restarts it again. This can be useful if something else interfered with
        /// the AudioSource.
        /// Triggering this can cause cracking sounds.
        /// </summary>
        public void ResetMicrophone()
        {
            Microphone.End(_MicrophoneName);
            mAudioSource.Stop();
            var clip = Microphone.Start(_MicrophoneName, true, 1, SampleRate);
            //stall until the audio starts
            while (!( Microphone.GetPosition(_MicrophoneName) > 0)) { }
            mAudioSource.clip = clip;
            mAudioSource.Play();
        }

        public void IncreaseDelay(float timeDif)
        {
            if (timeDif <= 0.0f || timeDif >= 1.0f)
                throw new ArgumentException("Changes in delay must be above 0 and below 1");
            float time = mAudioSource.time - timeDif;
            if (time < 0)
                time += 1;
            mAudioSource.time = time;
        }
        public void ReduceDelay(float timeDif)
        {
            if (timeDif <= 0.0f || timeDif >= 1.0f)
                throw new ArgumentException("Changes in delay must be above 0 and below 1");
            float time = mAudioSource.time + MinBufferAdjustment;
            if (time >= 1)
                time -= 1;
            mAudioSource.time = time;
        }

        private void Update()
        {
            if (mIsRecording)
            {
                int micPosition = Microphone.GetPosition(_MicrophoneName);
                float sourceTime = mAudioSource.time;
                mDelay = CalcDelay(micPosition / (float)SampleRate, sourceTime);

                if (MonitorDelay && mIsRecording)
                {
                    if(mDelay < MinBufferTime)
                    {
                        IncreaseDelay(MinBufferAdjustment);
                        if (DEBUG)
                            Debug.Log("Increased delay by " + MinBufferAdjustment + ". Delay was: " + mDelay);
                    }
                    if(mDelay > MaxBufferTime)
                    {
                        ReduceDelay(MinBufferAdjustment);
                        if(DEBUG)
                            Debug.Log("Reduced delay by " + MinBufferAdjustment + ". Delay was: " + mDelay);
                    }
                }
            }
        }
        protected override void StopRecording()
        {
            if (mIsRecording)
            {
                if (DEBUG)
                    Debug.Log("Stopping access to hardware microphone " + _MicrophoneName);
                mAudioSource.Stop();
                Microphone.End(_MicrophoneName);
                mIsRecording = false;
                Destroy(mAudioSource);
            }
        }
    }


    /// <summary>
    /// Additional debug information for use in UnityEditor 
    /// + checkboxes to manually test lower / higher delay or reset.
    /// </summary>
    public class MicrophoneDebugHelper : MonoBehaviour
    {
        /// <summary>
        /// Current position of the microphone buffer in samples
        /// </summary>
        public int micPosition;
        /// <summary>
        /// Current position of the AudioSource in samples
        /// </summary>
        public int sourcePosition;

        /// <summary>
        /// Checking this triggers a reset
        /// </summary>
        public bool triggerReset = false;

        /// <summary>
        /// Checking this reduces the delay
        /// (turn off monitoring first)
        /// </summary>
        public bool reduceDelay = false;
        /// <summary>
        /// Checking this increases the delay
        /// (turn off monitoring first)
        /// </summary>
        public bool increaseDelay = false;

        /// <summary>
        /// Smoothed delay value
        /// </summary>
        public float SmoothDelay = 0;

        private MicrophoneAudioInput mInput;
        private void Awake()
        {
            mInput = GetComponent<MicrophoneAudioInput>();
        }

        private void Update()
        {
            if (mInput && mInput.IsRecording)
            {
                micPosition = Microphone.GetPosition(mInput._MicrophoneName);
                sourcePosition = mInput.AudioSource.timeSamples;

                SmoothDelay = (mInput.Delay + SmoothDelay) / 2;
                if (triggerReset)
                {
                    triggerReset = false;
                    mInput.ResetMicrophone();
                }

                if (reduceDelay)
                {
                    mInput.ReduceDelay(mInput.MinBufferAdjustment);
                    reduceDelay = false;
                }
                if (increaseDelay)
                {
                    mInput.IncreaseDelay(mInput.MinBufferAdjustment);
                    increaseDelay = false;
                }
            }
        }
    }
}

