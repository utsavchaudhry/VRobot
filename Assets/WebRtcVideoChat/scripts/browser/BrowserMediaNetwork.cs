/* 
 * Copyright (C) 2022 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using Byn.Awrtc.Unity;
using UnityEngine;

namespace Byn.Awrtc.Browser
{
    public class BrowserMediaNetwork : BrowserWebRtcNetwork, IMediaNetwork
    {
        private FramePixelFormat mFormat = FramePixelFormat.ABGR;

        public BrowserMediaNetwork(NetworkConfig lNetConfig)
        {

            string conf = CAPI.NetworkConfigToJson(lNetConfig);

            SLog.L("Creating BrowserMediaNetwork config: " + conf, this.GetType().Name);
            mReference = CAPI.Unity_MediaNetwork_Create(conf);
        }


        private void SetOptional(int? opt, ref int value)
        {
            if (opt.HasValue)
            {
                value = opt.Value;
            }
        }

        private string CheckString(string opt)
        {
            if (string.IsNullOrEmpty(opt))
            {
                return "";
            }
            else
            {
                return opt;
            }
        }

        public void Configure(MediaConfig config)
        {
            mFormat = config.Format;
            int minWidth = -1;
            int minHeight = -1;
            int maxWidth = -1;
            int maxHeight = -1;
            int idealWidth = -1;
            int idealHeight = -1;
            int minFrameRate = -1;
            int maxFrameRate = -1;
            int idealFrameRate = -1;

            SetOptional(config.MinWidth, ref minWidth);
            SetOptional(config.MinHeight, ref minHeight);
            SetOptional(config.MaxWidth, ref maxWidth);
            SetOptional(config.MaxHeight, ref maxHeight);
            SetOptional(config.IdealWidth, ref idealWidth);
            SetOptional(config.IdealHeight, ref idealHeight);

            SetOptional(config.MinFrameRate, ref minFrameRate);
            SetOptional(config.MaxFrameRate, ref maxFrameRate);
            SetOptional(config.IdealFrameRate, ref idealFrameRate);

            string[] videoCodecs = new string[0];
            if(config.VideoCodecs != null && config.VideoCodecs.Length > 0){
                videoCodecs = config.VideoCodecs;
            }
            int videoBitrateKbits = -1;
            SetOptional(config.VideoBitrateKbits, ref videoBitrateKbits);


            string videoContentHint = CheckString(config.VideoContentHint);

            string audioInputDevice = "";
            if (config is BrowserMediaConfig)
            {
                var bconfig = config as BrowserMediaConfig;
                audioInputDevice = CheckString(bconfig.AudioInputDevice);
            }

                
            CAPI.Unity_MediaNetwork_Configure(mReference,
                config.Audio, config.Video,
                minWidth, minHeight,
                maxWidth, maxHeight,
                idealWidth, idealHeight,
                minFrameRate, maxFrameRate, idealFrameRate, config.VideoDeviceName,
                videoCodecs, videoCodecs.Length, videoBitrateKbits, videoContentHint, 
                audioInputDevice
            );
        }
        public IFrame TryGetFrame(ConnectionId id)
        {
            Texture2D buff = null;
            
            if (mFormat == FramePixelFormat.Native)
            {
                int[] width = new int[1];
                int[] height = new int[1];
                bool hasFrame = CAPI.Unity_MediaNetwork_TryGetFrame_Resolution(mReference, id.id, width, height);
                if (hasFrame == false)
                    return null;

                if (buff == null || buff.width != width[0] || buff.height != height[0])
                {
                    //must be in sync with RawFrame.ts
                    //RGB, mipmaps off
                    buff = new Texture2D(width[0], height[0], TextureFormat.RGB24, false);
                }
                int textureId = (int)buff.GetNativeTexturePtr();
                //

                bool res = CAPI.Unity_MediaNetwork_TryGetFrame_ToTexture(mReference, id.id, width[0], height[0], textureId);
                if (res == false)
                {
                    //this should never happen unless the browser is able to change the bufferd image between
                    //Unity_MediaNetwork_TryGetFrame_Resolution
                    //and the ToTexture call or there is a bug 
                    Debug.LogWarning("Skipped frame. Failed to move image into texture");
                    return null;
                }
                else
                {
                    return new TextureFrame(buff);
                }
            }
            else if (mFormat == FramePixelFormat.ABGR)
            {
                int length = CAPI.Unity_MediaNetwork_TryGetFrameDataLength(mReference, id.id);
                if (length < 0)
                    return null;

                int[] width = new int[1];
                int[] height = new int[1];
                byte[] buffer = new byte[length];

                bool res = CAPI.Unity_MediaNetwork_TryGetFrame(mReference, id.id, width, height, buffer, 0, buffer.Length);
                if (res)
                    return new BufferedFrame(buffer, width[0], height[0], FramePixelFormat.ABGR, 0, true);
                return null;
            }
            else
            {
                return null;
            }
        }

        public MediaConfigurationState GetConfigurationState()
        {
            int res = CAPI.Unity_MediaNetwork_GetConfigurationState(mReference);
            MediaConfigurationState state = (MediaConfigurationState)res;
            return state;
        }
        public override void Update()
        {
            base.Update();

        }
        public string GetConfigurationError()
        {
            if (GetConfigurationState() == MediaConfigurationState.Failed)
            {
                var err = CAPI.MediaNetwork_GetConfigurationError(mReference);
                return "" + err + " Check the browser log for more details.";
            }
            else
            {
                return null;
            }

        }

        public void ResetConfiguration()
        {
            CAPI.Unity_MediaNetwork_ResetConfiguration(mReference);
        }

        public void SetVolume(double volume, ConnectionId remoteUserId)
        {
            CAPI.Unity_MediaNetwork_SetVolume(mReference, volume, remoteUserId.id);
        }

        public bool HasAudioTrack(ConnectionId remoteUserId)
        {
            return CAPI.Unity_MediaNetwork_HasAudioTrack(mReference, remoteUserId.id);
        }

        public bool HasVideoTrack(ConnectionId remoteUserId)
        {
            return CAPI.Unity_MediaNetwork_HasVideoTrack(mReference, remoteUserId.id);
        }

        public bool IsMute()
        {
            return CAPI.Unity_MediaNetwork_IsMute(mReference);
        }

        public void SetMute(bool val)
        {
            CAPI.Unity_MediaNetwork_SetMute(mReference, val);
        }


        /// <summary>
        /// WebGL plugin specific feature! This call will allow setting the volume and panning the audio to the left or
        /// right speaker.  
        /// If Stereo is not supported the device  / browser might have its own way of handling this. It usually either
        /// merges both channels (panning is ignored) or it will only play the left or right channel. 
        /// 
        /// </summary>
        /// <param name="volume">
        /// 0 - no audio playback
        /// 1 - full volume
        /// The volume here is the same value as set via SetVolume.
        /// </param>
        /// <param name="pan">
        /// -1 playback only via the left speaker
        /// 0 balanced playback (default)
        /// 1 playback only via the right speaker
        /// </param>
        /// <param name="remoteUserId">
        /// Connection ID of the remote user to apply these settings to.
        /// </param>
        public void SetVolumePan(float volume, float pan, ConnectionId remoteUserId)
        {
            CAPI.Unity_MediaNetwork_SetVolumePan(mReference, volume, pan, remoteUserId.id);
        }
    }
}
