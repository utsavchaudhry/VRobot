using System;
using UnityEngine;
namespace Byn.Unity.Examples
{
    /// <summary>
    /// Captures audio from an attached AudioSource and sends it to a virtual
    /// VideoInput device. Attach this script to a GameObject and drag & drop onto
    /// the VirtualAudioInput reference of AudioInputCallApp for use.  
    /// </summary>
    public class UnityAudioInput : VirtualAudioInput
    {
        /// <summary>
        /// It isn't clear if all sample rates Unity supports also work with WebRTC. 
        /// It is recommended to stick with this one.
        /// </summary>
        const int RECOMMENDED_SAMPLE_RATE = 48000;

        /// <summary>
        /// If the goal is to never playback audio locally but just send it from the AudioSource attached
        /// set this to true.
        /// Some subclasses might set this to true automatically e.g. a microphone is not expected
        /// to trigger local playback.
        /// </summary>
        public bool _MutePlayback = false;

        /// <summary>
        /// If the goal is to playback audio locally but turn off playback when sending set this to true.
        /// </summary>
        public bool _MutePlaybackWhenAccessed = false;


        /// <summary>
        /// Current global sample rate configured in Unity AudioSettings
        /// </summary>
        private int mSampleRate = 0;
        public int SampleRate
        {
            get { return mSampleRate; }
        }

        /// <summary>
        /// Sample rate used when OnAudioFilterRead was last called
        /// </summary>
        private int mLastSampleRate = 0;

        /// <summary>
        /// Channel count when OnAudioFilterRead was last called
        /// </summary>
        private int mLastChannels = 0;



        protected override void Awake()
        {
            AudioSettings_OnAudioConfigurationChanged(false);
            AudioSettings.OnAudioConfigurationChanged += AudioSettings_OnAudioConfigurationChanged;
            base.Awake();
        }
        private void AudioSettings_OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            mSampleRate = AudioSettings.outputSampleRate;
        }

        protected override void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged -= AudioSettings_OnAudioConfigurationChanged;
            base.OnDestroy();
        }
        public void SetMutePlayback(bool val)
        {
            _MutePlayback = val;
        }
        public void SetMutePlaybackOnAccess(bool val)
        {
            _MutePlaybackWhenAccessed = val;
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
            //Skip 
            if (IsInitialized == false) return;

            //We log any changes in channels and sample rate in case it triggers any bugs
            if (channels != mLastChannels || mSampleRate != mLastSampleRate)
            {
                mLastSampleRate = mSampleRate;
                mLastChannels = channels;
                Debug.Log("Receiving sample rate " + mLastSampleRate + " and channels " + channels);

                //We feed audio data directly into WebRTC but what sample rates / channels are supported,
                //under what platforms / conditions isn't quite clear.
                //Try to stick with 48kHz if possible.
                if (mLastSampleRate != RECOMMENDED_SAMPLE_RATE)
                {
                    Debug.LogWarning("Sample rate " + mLastSampleRate + " hasn't been tested yet. Use at own risk.");
                }
                if (mLastChannels != 2)
                {
                    Debug.LogWarning("Channels are set to " + mLastChannels + ". This hasn't been tested yet. Use at own risk.");
                }
            }
            short[] samples = new short[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                samples[i] = (short)(data[i] * 32767);

                if (_MutePlayback || (_MutePlaybackWhenAccessed && IsAccessed) )
                    data[i] = 0;
            }
            if (IsAccessed)
                AddFrameData(samples, mLastSampleRate, mLastChannels);
        }
    }
}