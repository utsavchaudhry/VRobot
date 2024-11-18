using Byn.Awrtc;
using Byn.Unity.Examples;
using UnityEngine;
namespace Byn.Unity.Examples
{
    /// <summary>
    /// Ui to update / get settings for video and audio. 
    /// </summary>
    public class MediaConfigUi : MonoBehaviour
    {
        /// <summary>
        /// UI for video configuration
        /// </summary>
        public MediaTrackConfigurator _VideoConfig;
        /// <summary>
        /// UI for audio configuration
        /// </summary>
        public MediaTrackConfigurator _AudioConfig;

        /// <summary>
        /// Refresh UI based on the app settings
        /// </summary>
        /// <param name="app"></param>
        public void RefreshUi(WebGlAudioCallApp app)
        {
            Debug.Log("Refresh UI");
            _VideoConfig.RefreshFrom(app);
            _AudioConfig.RefreshFrom(app);
        }

        /// <summary>
        /// Store settings into the given MediaConfig for use.
        /// </summary>
        /// <param name="config">
        /// MediaConfig (assumed to be BrowserMediaConfig on WebGL)
        /// </param>
        public void CopySettingsTo(MediaConfig config)
        {
            _VideoConfig.CopyTo(config);
            _AudioConfig.CopyTo(config);
        }
    }
}