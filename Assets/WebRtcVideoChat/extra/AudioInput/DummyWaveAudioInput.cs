using System;
using UnityEngine;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Generates a sinus wave and sends it into the plugin for use as local audio source. 
    /// Attach this script to a GameObject and drag & drop onto the VirtualAudioInput reference of 
    /// AudioInputCallApp. 
    /// </summary>
    public class DummyWaveAudioInput : VirtualAudioInput
    {

        public readonly bool VERBOSE = false;
        public int sampleRate = 48000;
        public int channels = 2;
        public int frequency = 400;

        /// <summary>
        /// Keeps track of the place in the sine wave we are
        /// </summary>
        private int mTimeMs = 0;

        /// <summary>
        /// Keeps track of time since last generation
        /// </summary>
        private double mTimeCounter = 0;


        void Update()
        {
            if (IsInitialized == false || IsAccessed == false)
                return;
            //in ms.
            mTimeCounter += Time.deltaTime * 1000;

            int numberOfFrames = sampleRate / 100;
            int numberOfSamples = numberOfFrames * channels;
            short[] audio = new short[numberOfSamples];



            //for every full 10ms we generate one array with audio data. 
            for (; mTimeCounter > IDEAL_CHUNK_SIZE_MS; mTimeCounter -= IDEAL_CHUNK_SIZE_MS)
            {
                int res = GenTestAudio(frequency, sampleRate, channels, mTimeMs, audio);
                if (res != numberOfSamples)
                {
                    Debug.LogWarning("GenTestAudio malfunctioned");
                    this.enabled = false;
                    break;
                }
                mTimeMs += IDEAL_CHUNK_SIZE_MS;

                int success = AddFrameData(audio, sampleRate, channels);
                if (success < 0)
                    Debug.LogWarning("AudioInput.UpdateFrame dropped an update for " + DeviceName);
            }
        }

        public int GenTestAudio(float frequency, int sampleRate, int channels, long time_offset, short[] dst)
        {
            const double volume = 0.125;
            int requestedFrames = dst.Length / channels;

            //calculating in 10ms increments here. This way 44100 uses 441 frames
            int framesPer10ms = sampleRate / 100;
            long frameoffset = (time_offset / 10) * framesPer10ms;

            //TODO: frame offset currently keeps growing. at some point the floating point operations below
            //might not work reliably
            const double two_pi = Math.PI * 2;

            double increment = (two_pi * frequency / (double)sampleRate);
            int samplesGenerated = 0;
            for (int s = 0; s < requestedFrames; s++)
            {
                long sample = (frameoffset + s);
                double p = increment * sample;

                //calculator amplitude reduced by volume factor
                double amplitude = Math.Sin(p) * volume;

                //conversion to short
                short res = (short)(amplitude * short.MaxValue);

                //same volume for all channels
                for (int c = 0; c < channels; c++)
                {
                    dst[samplesGenerated] = res;
                    samplesGenerated++;
                }
            }
            //return samples written to the buffer
            return samplesGenerated;
        }
    }
}