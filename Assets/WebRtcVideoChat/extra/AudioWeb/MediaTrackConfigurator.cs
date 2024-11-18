using Byn.Awrtc;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Media type.
    /// </summary>
    public enum MediaUiType
    {
        VideoInput,
        AudioInput
    }
    /// <summary>
    /// Class for audio and video configurator
    /// </summary>
    public class MediaTrackConfigurator : MonoBehaviour
    {
        /// <summary>
        /// Toggle to enable / disable sending media
        /// </summary>
        public Toggle _EnabledToggle;
        /// <summary>
        /// Device selection dropdown
        /// </summary>
        public Dropdown _DeviceDropdown;
        /// <summary>
        /// Button to refresh device list
        /// </summary>
        public Button _RefreshButton;

        /// <summary>
        /// Video / audio
        /// </summary>
        public MediaUiType _MediaType;
        private WebGlAudioCallApp mApp;


        private List<KeyValuePair<string, string>> mCurrentOptions;

        private void Awake()
        {
            if (_EnabledToggle == null)
                Debug.LogWarning("_EnabledToggle object not set");
            if (_DeviceDropdown == null)
                Debug.LogWarning("_DeviceDropdown object not set");

            _RefreshButton.onClick.AddListener(Refresh);
        }

        public void RefreshFrom(WebGlAudioCallApp app)
        {
            mApp = app;
            UpdateDeviceList();
        }

        /// <summary>
        /// Refreshes the device list
        /// </summary>
        public void Refresh()
        {
            UpdateDeviceList();
        }


        private bool CanSelectDevice()
        {
            if (this._MediaType == MediaUiType.AudioInput)
            {
                return mApp.CanSelectAudioInputDevice();
            }
            else if (this._MediaType == MediaUiType.VideoInput)
            {
                return mApp.CanSelectVideoDevice();
            }
            return false;
        }

        private List<KeyValuePair<string, string>> GetDevices()
        {
            if (this._MediaType == MediaUiType.AudioInput)
            {
                return mApp.GetAudioInputDevices();
            }
            else if (this._MediaType == MediaUiType.VideoInput)
            {
                return mApp.GetVideoInputDevices();
            }
            //no selection. we disable it and just show an empty dropdown
            var res = new List<KeyValuePair<string, string>>();
            res.Add(KeyValuePair.Create("", ""));
            return res;
        }
        private void UpdateDeviceList()
        {

            _DeviceDropdown.ClearOptions();

            bool canSelect = CanSelectDevice();
            _DeviceDropdown.interactable = canSelect;
            if (canSelect)
            {
                mCurrentOptions = GetDevices();
                List<string> devices = new List<string>();
                foreach (var v in mCurrentOptions)
                {
                    devices.Add(v.Value);
                }
                _DeviceDropdown.AddOptions(new List<string>(devices));
            }
        }
        /// <summary>
        /// Copies UI configuration into MediaConfig for use.
        /// </summary>
        /// <param name="config"></param>
        public void CopyTo(MediaConfig config)
        {
            if (this._MediaType == MediaUiType.AudioInput)
            {
                config.Audio = _EnabledToggle.isOn;

#if UNITY_WEBGL //&& !UNITY_EDITOR

            if (_DeviceDropdown.value <= 0)
            {
                //0 is any in which case we don't set a video device and let the platform specific layers figure it out
                (config as Byn.Awrtc.Browser.BrowserMediaConfig).AudioInputDevice = null;
            }
            else
            {
                (config as Byn.Awrtc.Browser.BrowserMediaConfig).AudioInputDevice = mCurrentOptions[_DeviceDropdown.value].Key;
            }
#endif
            }
            else if (this._MediaType == MediaUiType.VideoInput)
            {
                config.Video = _EnabledToggle.isOn;

                if (_DeviceDropdown.value <= 0)
                {
                    //0 is any in which case we don't set a video device and let the platform specific layers figure it out
                    config.VideoDeviceName = null;
                }
                else
                {
                    config.VideoDeviceName = mCurrentOptions[_DeviceDropdown.value].Key;
                }
            }

        }
    }
}