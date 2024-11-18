/* 
 * Copyright (C) 2024 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace Byn.Awrtc.Browser
{
    //These call go via awrtc_unity.jslib to awrtc.jspre which has the same calls but starting with CAPI_
    public static class CAPI
    {

        [DllImport("__Internal")]
        public static extern void Unity_BrowserCallFactory_InjectJsCode(string jscode);


        #region WebRtcNetwork CAPI imports
        [DllImport("__Internal")]
        public static extern bool Unity_WebRtcNetwork_IsAvailable();

        [DllImport("__Internal")]
        public static extern bool Unity_WebRtcNetwork_IsBrowserSupported();

        [DllImport("__Internal")]
        public static extern int Unity_WebRtcNetwork_Create(string lConfiguration);

        [DllImport("__Internal")]
        public static extern void Unity_WebRtcNetwork_Release(int lIndex);

        [DllImport("__Internal")]
        public static extern int Unity_WebRtcNetwork_Connect(int lIndex, string lRoom);

        [DllImport("__Internal")]
        public static extern void Unity_WebRtcNetwork_StartServer(int lIndex, string lRoom);
        [DllImport("__Internal")]
        public static extern void Unity_WebRtcNetwork_StopServer(int lIndex);

        [DllImport("__Internal")]
        public static extern void Unity_WebRtcNetwork_Disconnect(int lIndex, int lConnectionId);

        [DllImport("__Internal")]
        public static extern void Unity_WebRtcNetwork_Shutdown(int lIndex);
        [DllImport("__Internal")]
        public static extern void Unity_WebRtcNetwork_Update(int lIndex);
        [DllImport("__Internal")]
        public static extern void Unity_WebRtcNetwork_Flush(int lIndex);

        [DllImport("__Internal")]
        public static extern bool Unity_WebRtcNetwork_SendData(int lIndex, int lConnectionId, byte[] lUint8ArrayDataPtr, int lUint8ArrayDataOffset, int lUint8ArrayDataLength, bool lReliable);

        [DllImport("__Internal")]
        public static extern int Unity_WebRtcNetwork_GetBufferedAmount(int lIndex, int lConnectionId, bool lReliable);

        [DllImport("__Internal")]
        public static extern int Unity_WebRtcNetwork_PeekEventDataLength(int lIndex);

        [DllImport("__Internal")]
        public static extern bool Unity_WebRtcNetwork_Dequeue(int lIndex,
            int[] lTypeIntArrayPtr,
            int[] lConidIntArrayPtr,
            byte[] lUint8ArrayDataPtr, int lUint8ArrayDataOffset, int lUint8ArrayDataLength,
            int[] lDataLenIntArray);
        [DllImport("__Internal")]
        public static extern bool Unity_WebRtcNetwork_Peek(int lIndex,
            int[] lTypeIntArrayPtr,
            int[] lConidIntArrayPtr,
            byte[] lUint8ArrayDataPtr, int lUint8ArrayDataOffset, int lUint8ArrayDataLength,
            int[] lDataLenIntArray);



        [DllImport("__Internal")]
        public static extern int Unity_WebRtcNetwork_RequestStats(int lIndex);

        [DllImport("__Internal")]
        public static extern IntPtr Unity_WebRtcNetwork_DequeueRtcEvent(int lIndex, int[] lTypeIntArrayPtr, int[] lConidIntArrayPtr);

        [DllImport("__Internal")]
        public static extern IntPtr Unity_WebRtcNetwork_DequeueRtcEvent_Release(IntPtr ptr);


        #endregion


        #region MediaNetwork CAPI imports
        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_IsAvailable();

        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_HasUserMedia();

        [DllImport("__Internal")]
        public static extern int Unity_MediaNetwork_Create(string lJsonConfiguration);

        [DllImport("__Internal")]
        public static extern void Unity_MediaNetwork_Configure(int lIndex, bool audio, bool video,
            int minWidth, int minHeight, int maxWidth, int maxHeight, int idealWidth, int idealHeight,
            int minFps, int maxFps, int idealFps, string videoDeviceName,
            string[] videoCodecsPtr, int videoCodecsLen, int videoBitrateKbits, string videoContentHint, 
            string audioInputDevice);

        [DllImport("__Internal")]
        public static extern int Unity_MediaNetwork_GetConfigurationState(int lIndex);

        [DllImport("__Internal")]
        public static extern int Unity_MediaNetwork_GetConfigurationError_Length(int lIndex);

        [DllImport("__Internal")]
        public static extern void Unity_MediaNetwork_GetConfigurationError(int lIndex, byte[] bufferPtr, int buffLen);

        public static string MediaNetwork_GetConfigurationError(int lIndex){
            int charLength = Unity_MediaNetwork_GetConfigurationError_Length(lIndex);
            if(charLength == 0)
                return null;

            //oversized buffer
            int bufflen = charLength * 4;
            byte[] buffer = new byte[bufflen];            
            Unity_MediaNetwork_GetConfigurationError(lIndex, buffer, bufflen);
            string result = Encoding.UTF8.GetString(buffer);
            return result;
        }

        [DllImport("__Internal")]
        public static extern void Unity_MediaNetwork_ResetConfiguration(int lIndex);


        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_TryGetFrame(int lIndex, int connectionId, int[] lWidth, int[] lHeight, byte[] lBuffer, int offset, int length);


        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_TryGetFrame_ToTexture(int lIndex, int connectionId, int lWidth, int lHeight, int lTextureId);
        //[DllImport("__Internal")]
        //public static extern bool Unity_MediaNetwork_TryGetFrame_ToTexture2(int lIndex, int connectionId, int[] lWidth, int[] lHeight, int[] lTextureId);


        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_TryGetFrame_Resolution(int lIndex, int connectionId, int[] lWidth, int[] lHeight);

        [DllImport("__Internal")]
        public static extern int Unity_MediaNetwork_TryGetFrameDataLength(int lIndex, int connectionId);


        [DllImport("__Internal")]
        public static extern void Unity_MediaNetwork_SetVolume(int lIndex, double volume, int connectionId);


        [DllImport("__Internal")]
        public static extern void Unity_MediaNetwork_SetVolumePan(int lIndex, double volume, double pan, int connectionId);

        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_HasAudioTrack(int lIndex, int connectionId);

        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_HasVideoTrack(int lIndex, int connectionId);

        [DllImport("__Internal")]
        public static extern void Unity_MediaNetwork_SetMute(int lIndex, bool value);

        [DllImport("__Internal")]
        public static extern bool Unity_MediaNetwork_IsMute(int lIndex);

        #endregion
        //Device API
        [DllImport("__Internal")]
        public static extern void Unity_DeviceApi_Update();
        [DllImport("__Internal")]
        public static extern void Unity_DeviceApi_RequestUpdate();
        [DllImport("__Internal")]
        public static extern ulong Unity_DeviceApi_LastUpdate();
        [DllImport("__Internal")]
        public static extern uint Unity_Media_GetVideoDevices_Length();
        [DllImport("__Internal")]
        public static extern int Unity_Media_GetVideoDevices(int index, byte[] bufferPtr, int buffLen);
        [DllImport("__Internal")]
        public static extern uint Unity_Media_GetAudioInputDevices_Length();
        [DllImport("__Internal")]
        public static extern int Unity_Media_GetAudioInputDevices(int index, byte[] bufferPtr, int buffLen);

        //video input api
        [DllImport("__Internal")]
        public static extern void Unity_VideoInput_AddDevice(string name, int width, int height, int fps);
        [DllImport("__Internal")]
        public static extern void Unity_VideoInput_RemoveDevice(string name);

        //rotation & firstRowIsBottom might not be supported and are ignored on the java script side (not needed in WebGL anyway)
        [DllImport("__Internal")]
        public static extern bool Unity_VideoInput_UpdateFrame(string name, byte[] lBuffer, int offset, int length, int width, int height, int rotation, bool firstRowIsBottom);

        //rotation & firstRowIsBottom might not be supported and are ignored on the java script side (not needed in WebGL anyway)
        [DllImport("__Internal")]
        public static extern bool Unity_VideoInput_UpdateFrame(string name, IntPtr ptr, int offset, int length, int width, int height, int rotation, bool firstRowIsBottom);

        [DllImport("__Internal")]
        public static extern void Unity_Media_EnableScreenCapture(string name, bool captureAudio);
        
        //Adds an HTMLCanvasElement to be treated like a video device. query is used to find the canvas in the page using
        //document.querySelector in java script.
        [DllImport("__Internal")]
        public static extern bool Unity_VideoInput_AddCanvasDevice(string query, string name, int width, int height, int fps);


        //SLog
        /// <summary>
        /// Verbose = 0
        /// Info = 1,
        /// Warnings = 2,
        /// Errors = 3,
        /// None = 4
        /// </summary>
        /// <param name="logLevel">Value for the corresponding log level.</param>
        [DllImport("__Internal")]
        public static extern void Unity_SLog_SetLogLevel(int logLevel);

        public enum InitMode : int
        {
            //Original mode. Devices will be unknown after startup
            Default = 0,
            //Waits for the desvice info to come in
            //names might be missing though (browser security thing)
            WaitForDevices = 1,
            //Asks the user for camera / audio access to be able to
            //get accurate device information
            RequestAccess = 2
        };
        public enum InitState : int
        {
            Uninitialized = 0,
            Initializing = 1,
            Initialized = 2,
            Failed = 3
        };
        [DllImport("__Internal")]
        public static extern void Unity_InitAsync(InitMode initmode, bool useWebRtcAdapter);

        [DllImport("__Internal")]
        public static extern InitState Unity_PollInitState();


        public static string ToJson(bool res)
        {
            if (res)
                return "true";
            return "false";
        }
        public static string NetworkConfigToJson(NetworkConfig config)
        {

            IceServer[] iceServers = null;
            if (config.IceServers != null)
            {
                iceServers = config.IceServers.ToArray();
            }


            StringBuilder iceServersJson = new StringBuilder();
            IceServersToJson(iceServers, iceServersJson);

            //e.g. '{"IceServers":[{"urls":["turn.y-not.app:12345"],"credential":"testpass","username":"testuser"}],"SignalingUrl":"ws://s.y-not.app","IsConference":true,"MaxIceRestart":2,"KeepSignalingAlive":true}';

            string conf = "{\"IceServers\":" + iceServersJson
                + ",\"SignalingUrl\":\"" + config.SignalingUrl
                + "\",\"IsConference\":" + ToJson(config.IsConference)
                + ",\"MaxIceRestart\":" + config.MaxIceRestart
                + ",\"KeepSignalingAlive\":" + ToJson(config.KeepSignalingAlive) + "}";


            return conf;
        }

        static void IceServersToJson(IceServer[] lIceServers, StringBuilder jsout)
        {
            if (lIceServers == null)
            {
                jsout.Append("null");
            }
            else if (lIceServers.Length == 0)
            {
                jsout.Append("[]");
            }
            else
            {

                jsout.Append("["); //start iceServers array
                for (int i = 0; i < lIceServers.Length; i++)
                {
                    if (i > 0)
                    {
                        jsout.Append(",");
                    }
                    jsout.Append("{"); // start iceServers[i] object


                    //urls field is an array of strings for each url iceServers[i].urls
                    jsout.Append("\"");
                    jsout.Append("urls");
                    jsout.Append("\"");
                    jsout.Append(":");
                    if (lIceServers[i].Urls == null)
                    {
                        jsout.Append("null");
                    }
                    else if (lIceServers[i].Urls.Count == 0)
                    {
                        jsout.Append("[]");
                    }
                    else
                    {
                        jsout.Append("[");
                        for (int k = 0; k < lIceServers[i].Urls.Count; k++)
                        {
                            if (k > 0)
                            {
                                jsout.Append(",");
                            }
                            jsout.Append("\"");
                            jsout.Append(lIceServers[i].Urls[k]);
                            jsout.Append("\"");
                        }
                        jsout.Append("]");//end iceServers[i].urls array
                    }

                    if (lIceServers[i].Username != null)
                    {
                        jsout.Append(",");
                        jsout.Append("\"");
                        jsout.Append("username");
                        jsout.Append("\"");
                        jsout.Append(":");
                        jsout.Append("\"");
                        jsout.Append(lIceServers[i].Username);
                        jsout.Append("\"");
                    }
                    if (lIceServers[i].Credential != null)
                    {
                        jsout.Append(",");
                        jsout.Append("\"");
                        jsout.Append("credential");
                        jsout.Append("\"");
                        jsout.Append(":");
                        jsout.Append("\"");
                        jsout.Append(lIceServers[i].Credential);
                        jsout.Append("\"");
                    }

                    jsout.Append("}");// end iceServers[i] object
                }
                jsout.Append("]"); // end iceServers array
            }
        }
    }
}