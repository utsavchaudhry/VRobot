using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebRtcCSharp;


namespace Byn.Unity.Examples
{
    /// <summary>
    /// The UnityMicrophoneManager will create virtual audio devices on the fly that match
    /// the physical microphones. 
    /// </summary>
    public class UnityMicrophoneManager : MonoBehaviour, IAudioInputDependency
    {
        private AudioInputExample mFactory;
        private List<MicrophoneAudioInput> mAudioDevices = new List<MicrophoneAudioInput>();
        public void SetupDevice(AudioInput input)
        {
            mFactory = AudioInputExample.Instance;

            foreach( var device in Microphone.devices )
            {
                var mic = SpawnMicrophone(device);
                mAudioDevices.Add(mic);
                mFactory.AddDevice(mic);
            }
        }

        private MicrophoneAudioInput SpawnMicrophone(string device)
        {

            GameObject go = new GameObject(device);
            go.transform.SetParent(this.gameObject.transform);
            var mic = go.AddComponent<MicrophoneAudioInput>();
            //name of the microphone to access
            mic._MicrophoneName = device;
            //name used to address it
            mic._DeviceName = "mic_" + device;
            return mic;
        }
        public void DestroyDevice()
        {
            mAudioDevices.Clear();
            mFactory = null;
        }

    }
}