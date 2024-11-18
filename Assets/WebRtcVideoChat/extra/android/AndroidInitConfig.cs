/* 
 * Copyright (C) 2022 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
namespace Byn.Awrtc.Unity
{
    /// <summary>
    /// Use only for testing! Devices & Android versions behave very differently on each device and
    /// setting any flags will most likely break the asset on the majority of devices.
    /// 
    /// Custom Android specific flags to change how WebRTC's 
    /// PeerConnectionFactory is initialized
    /// 
    /// Flags must be set to UnityCallFactory.AndroidConfig before the first use of
    /// UnityCallFactory.Instance or UnityCallFactory.EnsureInit
    /// 
    /// 
    /// </summary>
    public class AndroidInitConfig
    {
        /// <summary>
        /// True will allow using Androids hardware acceleration.
        /// They often do not work though due to lack of support / some do return image formats the
        /// internals can't process. The result can be very low FPS or crashes.
        /// </summary>
        public bool hardwareAcceleration = false;
        /// <summary>
        /// Some HW codecs on some devices can only output to textures. These devices might allow using hw codecs if useTexture = true
        /// Others might fail if useTextures=true 
        /// </summary>
        public bool useTextures = false;

        /// <summary>
        /// Allows setting a set VP8, VP9 or H264 as preferred codec. 
        /// H264 only works if hardware codecs are active.
        /// </summary>
        public string preferredCodec = null;

        /// <summary>
        /// True will treat every codec as not supported except the one set as preferredCodec. 
        /// </summary>
        public bool forcePreferredCodec = false;

        public override string ToString()
        {
            return "{hardwareAcceleration:" + hardwareAcceleration + ", useTextures:" + useTextures
                + ", preferredCodec:" + preferredCodec + ", forcePreferredCodec:" + forcePreferredCodec + "}";
        }
    }
}
