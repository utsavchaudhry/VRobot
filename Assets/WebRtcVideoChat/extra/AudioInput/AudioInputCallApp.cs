using Byn.Awrtc;
using Byn.Awrtc.Native;
using Byn.Awrtc.Unity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Byn.Unity.Examples
{

    /// <summary>
    /// Custom CallApp that will use the new AudioInput api. 
    /// For now this API is not yet built into UnityCallFactory. Instead these tasks are done by
    /// the class AudioInputExample
    /// </summary>
    public class AudioInputCallApp : CallApp
    {
        /// <summary>
        /// UI element to let the user pick an audio device
        /// </summary>
        public Dropdown _AudioDropdown;


        private string mSelectedDevice = null;

        /// <summary>
        /// We replace the original Start method to instead wait
        /// for the init of our custom factory.
        /// </summary>
        protected override void Start()
        {
            //to trigger android permission requests
            StartCoroutine(ExampleGlobals.RequestPermissions());

            AudioInputExample.EnsureInit(OnCallFactoryReady, OnCallFactoryFailed);
        }
        protected override void OnCallFactoryReady()
        {
            base.OnCallFactoryReady();

            RefreshAudioDropdown();
            Debug.Log("AudioInputCallApp ready");
        }
        protected override void OnCallFactoryFailed(string error)
        {
            base.OnCallFactoryFailed(error);
        }

        private void RefreshAudioDropdown()
        {
            var devs = new List<string>(AudioInputExample.Instance.GetAudioInputDevices());
            _AudioDropdown.ClearOptions();
            _AudioDropdown.AddOptions(devs);
        }

        private string GetSelectedAudioDevice()
        {
            if (_AudioDropdown.value < 0 || _AudioDropdown.value >= _AudioDropdown.options.Count)
            {
                return null;
            }
            else
            {
                string devname = _AudioDropdown.options[_AudioDropdown.value].text;
                return devname;
            }
        }

        /// <summary>
        /// Make sure that the default CallApp example is using our custom factory.
        /// </summary>
        /// <param name="netConfig"></param>
        /// <returns></returns>
        protected override ICall CreateCall(NetworkConfig netConfig)
        {
            //using custom factory to create the call
            return AudioInputExample.Instance.CreateCall(netConfig);
        }



        public override void Configure()
        {
            var nativeMediaConfig = mMediaConfig as NativeMediaConfig;
            if (nativeMediaConfig != null)
            {
                mSelectedDevice = GetSelectedAudioDevice();
                Debug.Log("Setting audio device to "+ mSelectedDevice);
                nativeMediaConfig.AudioDeviceName = mSelectedDevice;
                nativeMediaConfig.AudioOptions.echo_cancellation = false;
            }
            else
            {
                Debug.LogError("MediaConfig wasn't instance of NativeMediaConfig. Unsupported platform?");
            }


            //Configure is triggered after call creation and before the first attempted access to audio devices
            AudioInputExample.Instance.StartAccessDevice(mSelectedDevice);
            base.Configure();
        }

        protected override void CleanupCall()
        {
            //Cleanup on disconnect or any other shutdown. The device won't be needed after that
            base.CleanupCall();
            AudioInputExample.Instance.StopAccessDevice(mSelectedDevice);
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log("AudioInputCallApp destroyed.");
        }
    }
}