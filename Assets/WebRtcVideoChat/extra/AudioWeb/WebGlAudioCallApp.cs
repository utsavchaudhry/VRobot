using Byn.Awrtc;
using Byn.Awrtc.Browser;
using Byn.Awrtc.Unity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Custom callapp to allow for audio device selection. 
    /// Also testing a more modular UI system here. 
    /// </summary>
    public class WebGlAudioCallApp : CallApp
    {
        private UnityCallFactory mFactory;

        #region audio device selection
        public MediaConfigUi _Ui2;
        #endregion

        #region web audio panning
        /// <summary>
        /// This can pan the received audio (if the browser, platform and device supports it).
        /// </summary>
        public Slider _AudioPanner;
        private float mVolume = 1;
        #endregion

        protected override void Awake()
        {
            base.Awake();
            InitPanning();
        }

        protected override void OnCallFactoryReady()
        {
            base.OnCallFactoryReady();
            //In preparation for allowing multiple parallel factories
            mFactory = UnityCallFactory.Instance;
            UpdateConfigUi();
        }

        #region audio device selection
        private void UpdateConfigUi()
        {
            _Ui2.RefreshUi(this);
        }

        /// <summary>
        /// This replaces the default cross platform MediaConfig with BrowserMediaConfig. 
        /// </summary>
        /// <returns></returns>
        public override MediaConfig CreateMediaConfig()
        {
            BrowserMediaConfig mediaConfig = new BrowserMediaConfig();
            mediaConfig.Audio = true;
            mediaConfig.Video = true;
            return mediaConfig;
        }
        public override void Configure()
        {
            _Ui2.CopySettingsTo(this.mMediaConfig);
            base.Configure();
        }

        /// <summary>
        /// Used by the UI
        /// </summary>
        /// <returns></returns>
        public bool CanSelectAudioInputDevice()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns a list of audio devices with ID.
        /// </summary>
        /// <returns>
        /// List with KeyValuePair. The Key is an ID of the device (if supported) and
        /// the Value the device name. If ID's are not known the ID will be a unique device name.
        /// </returns>
        public List<KeyValuePair<string, string>> GetAudioInputDevices()
        {
            List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();
            devices.Add(KeyValuePair.Create("", "Default"));
            if (CanSelectAudioInputDevice())
            {
#if UNITY_WEBGL //&& !UNITY_EDITOR
            var devs = (mFactory.InternalFactory as Byn.Awrtc.Browser.BrowserCallFactory).GetAudioInputDevices();
            foreach(var v in devs)
            {
                devices.Add(KeyValuePair.Create(v.Id, v.Name));
            }
#endif
            }
            return devices;
        }



        /// <summary>
        /// Returns a list of video devices with ID.
        /// </summary>
        /// <returns>
        /// List with KeyValuePair. The Key is an ID of the device (if supported) and
        /// the Value the device name. If ID's are not known the ID will be a unique device.
        /// </returns>
        public List<KeyValuePair<string, string>> GetVideoInputDevices()
        {
            List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();
            devices.Add(KeyValuePair.Create("", "Default"));
            if (CanSelectVideoDevice())
            {
#if UNITY_WEBGL //&& !UNITY_EDITOR
            var devs = mFactory.GetVideoDevices();
            foreach (var v in devs)
            {
                devices.Add(KeyValuePair.Create(v, v));
            }
#endif
            }
            return devices;
        }

        protected override void CleanupCall()
        {
            base.CleanupCall();
            UpdateConfigUi();
        }

        #endregion


        #region web audio panning
        private void InitPanning()
        {
            _AudioPanner.onValueChanged.AddListener(OnPanChanged);
        }
        /// <summary>
        /// We intercept the SetRemoteVolume call to keep the volume constant when
        /// using SetVolumePan. 
        /// </summary>
        /// <param name="volume"></param>
        public override void SetRemoteVolume(float volume)
        {
            this.mVolume = volume;
            base.SetRemoteVolume(volume);
        }

        /// <summary>
        /// Event handler for the panning slider. 
        /// </summary>
        /// <param name="pan"></param>
        public void OnPanChanged(float pan)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        (mCall as Byn.Awrtc.Browser.BrowserWebRtcCall).SetVolumePan(mVolume, pan, mRemoteUserId);
#else
            Debug.LogWarning("Panning not supported. Ignored pan value of " + pan);
#endif
        }
        #endregion
    }
}