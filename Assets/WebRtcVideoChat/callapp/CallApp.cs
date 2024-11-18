/* 
 * Copyright (C) 2022 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using Byn.Awrtc;
using Byn.Awrtc.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Main example and test app for usage of WebRTC Video Chat.
    /// It can be used directly as a plug and play example via the CallApp prefab.
    /// </summary>
    public class CallApp : MonoBehaviour, ICallApp
    {
        /// <summary>
        /// URL to the signaling server.  
        /// See https://github.com/because-why-not/awrtc_signaling to run your own.
        /// </summary>
        public string uSignalingUrl = ExampleGlobals.SignalingCallApp;


        /// <summary>
        /// Ice server is either a stun or a turn server used to get trough
        /// the firewall.
        /// Warning: make sure the URL is in a valid format and
        /// starts with stun: or turn:
        /// 
        /// WebRTC will try many different ways to connect the peers so if
        /// this server is not available it might still be able
        /// to establish a direct connection or use the second ice server.
        /// 
        /// If you need more than two servers change the CreateNetworkConfig
        /// method.
        /// </summary>
        public string uIceServer = ExampleGlobals.TurnUrl;

        //
        public string uIceServerUser = ExampleGlobals.TurnUser;
        public string uIceServerPassword = ExampleGlobals.TurnPass;

        /// <summary>
        /// Second ice server. As I can't guarantee the test server is always online.
        /// If you need more than two servers or username / password then
        /// change the CreateNetworkConfig method.
        /// </summary>
        public string uIceServer2 = ExampleGlobals.BackupStunUrl;


        /// <summary>
        /// Do not change. This length is enforced on the server side to avoid abuse.
        /// </summary>
        public const int MAX_CODE_LENGTH = 256;

        /// <summary>
        /// Call class handling all the functionality
        /// </summary>
        protected ICall mCall;


        /// <summary>
        /// The UI is in a separate MonoBehaviour
        /// </summary>
        protected CallAppUi mUi;

        /// <summary>
        /// Contains the configuration used for the next call
        /// </summary>
        protected MediaConfig mMediaConfig;

        //Configuration for the currently active call
        /// <summary>
        /// Set to true after Join is called.
        /// Set to false after either Join failed or the call
        /// ended / network failed / user exit
        /// 
        /// </summary>
        protected bool mCallActive = false;
        public bool IsCallActive
        {
            get
            {
                return mCallActive;
            }
        }
        protected string mUseAddress = null;
        protected MediaConfig mMediaConfigInUse;
        protected ConnectionId mRemoteUserId = ConnectionId.INVALID;


        protected bool mAutoRejoin = false;
        protected IEnumerator mAutoRejoinCoroutine = null;
        protected float mRejoinTime = 2;

        protected bool mLocalFrameEvents = true;
        private FrameProcessor mFrameProcessor = null;


        #region Calls from unity
        //
        protected virtual void Awake()
        {
            mFrameProcessor = this.gameObject.AddComponent<FrameProcessor>();
            mFrameProcessor.FrameProcessed += OnFrameProcessed;
            mUi = GetComponent<CallAppUi>();
            mMediaConfig = CreateMediaConfig();
            mMediaConfigInUse = mMediaConfig;
        }

        protected virtual void Start()
        {
            //set to warning for regular use
            UnityCallFactory.RequestLogLevelStatic(UnityCallFactory.LogLevel.Info);
            //to trigger android permission requests
            StartCoroutine(ExampleGlobals.RequestPermissions());
            UnityCallFactory.EnsureInit(OnCallFactoryReady, OnCallFactoryFailed);
        }


        /// <summary>
        /// Called once the call factory is ready to be used.
        /// </summary>
        protected virtual void OnCallFactoryReady()
        {
            mUi.SetGuiState(true);

        }
        /// <summary>
        /// Called if the call factory failed to initialize.
        /// This is usually an asset configuration error, attempt to run a platform that isn't supported or the user
        /// managed to run the app while blocking video / audio access
        /// </summary>
        /// <param name="error">Error returned by the init process.</param>
        protected virtual void OnCallFactoryFailed(string error)
        {
            string fullErrorMsg = typeof(CallApp).Name + " can't start. The " + typeof(UnityCallFactory).Name + " failed to initialize with following error: " + error;
            Debug.LogError(fullErrorMsg);
        }


        protected virtual void OnDestroy()
        {
            CleanupCall();
        }

        /// <summary>
        /// The call object needs to be updated regularly to sync data received via webrtc with
        /// unity. All events will be triggered during the update method in the unity main thread
        /// to avoid multi threading errors
        /// </summary>
        protected virtual void Update()
        {
            if (mCall != null)
            {
                //update the call object. This will trigger all buffered events to be fired
                //to ensure it is done in the unity thread at a convenient time.
                mCall.Update();
            }
        }
        #endregion





        protected virtual NetworkConfig CreateNetworkConfig()
        {
            NetworkConfig netConfig = new NetworkConfig();
            if (string.IsNullOrEmpty(uIceServer) == false)
                netConfig.IceServers.Add(new IceServer(uIceServer, uIceServerUser, uIceServerPassword));
            if (string.IsNullOrEmpty(uIceServer2) == false)
                netConfig.IceServers.Add(new IceServer(uIceServer2));


            netConfig.SignalingUrl = uSignalingUrl;

            if (netConfig.SignalingUrl == "")
            {
                throw new InvalidOperationException("set signaling URL is empty");
            }

            return netConfig;
        }

        /// <summary>
        /// Creates the call object and uses the configure method to activate the 
        /// video / audio support if the values are set to true.
        /// generating new frames after this call so the user can see himself before
        /// the call is connected.
        /// </summary>
        public virtual void SetupCall()
        {
            Append("Setting up ...");

            //hacks to turn off certain connection types. If all set to true
            //the connection is forced to relay via the turn server (or fail none configured)
            //turns off direct connections through LAN / WiFi
            //Byn.Awrtc.Native.InternalDataPeer.sDebugIgnoreTypHost = true;
            //turns off STUN to directly connect online through NAT
            //Byn.Awrtc.Native.InternalDataPeer.sDebugIgnoreTypSrflx = true;
            //Byn.Awrtc.Native.InternalDataPeer.sDebugIgnoreTypPrflx = true;
            NetworkConfig netConfig = CreateNetworkConfig();



            mCall = CreateCall(netConfig);
            if (mCall == null)
            {
                Append("Failed to create the call");
                return;
            }

            mCall.LocalFrameEvents = mLocalFrameEvents;

            Append("Call created!");
            mCall.CallEvent += Call_CallEvent;

            Configure();

        }

        public virtual void Configure()
        {

            //make a deep clone to avoid confusion if settings are changed
            //at runtime. 
            mMediaConfigInUse = mMediaConfig.DeepClone();

            //try to pick a good default video device if the user wants to send video but
            //didn't bother to pick a specific device
            if (mMediaConfigInUse.Video && string.IsNullOrEmpty(mMediaConfigInUse.VideoDeviceName))
            {
                string[] devices = UnityCallFactory.Instance.GetVideoDevices();
                if (devices == null || devices.Length == 0)
                {
                    Debug.Log("no device found or no device information available");
                }
                else
                {
                    foreach (string s in devices)
                        Debug.Log("device found: " + s + " IsFrontFacing: " + UnityCallFactory.Instance.IsFrontFacing(s));
                }
                mMediaConfigInUse.VideoDeviceName = UnityCallFactory.Instance.GetDefaultVideoDevice();
            }


            mCall.Configure(mMediaConfigInUse);
            mUi.SetGuiState(false);
        }


        /// <summary>
        /// Just forwards the call to UnityCallFactory.
        /// Moved into its own method so you can create it differently
        /// via a subclass.
        /// </summary>
        protected virtual ICall CreateCall(NetworkConfig netConfig)
        {
            //setup the server
            return UnityCallFactory.Instance.Create(netConfig);
        }



        /// <summary>
        /// Handler of call events.
        /// 
        /// Can be customized in via subclasses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Call_CallEvent(object sender, CallEventArgs e)
        {
            switch (e.Type)
            {
                case CallEventType.CallAccepted:
                    //Outgoing call was successful or an incoming call arrived
                    Append("Connection established");
                    mRemoteUserId = ((CallAcceptedEventArgs)e).ConnectionId;
                    Debug.Log("New connection with id: " + mRemoteUserId
                        + " audio:" + mCall.HasAudioTrack(mRemoteUserId)
                        + " video:" + mCall.HasVideoTrack(mRemoteUserId));
                    break;
                case CallEventType.CallEnded:
                    {
                        //Call was ended / one of the users hung up -> reset the app
                        Append("Call ended");

                        var args = e as CallEndedEventArgs;
                        mFrameProcessor.FreeConnection(args.ConnectionId);
                        InternalResetCall();
                        break;
                    }
                case CallEventType.ListeningFailed:
                    //listening for incoming connections failed
                    //This is not an error!
                    //This usually means another user is already listening on that address waiting for us to connect
                    //try to create an outgoing connection instead
                    Debug.Log("Could not listen on address. Trying to connect to waiting user on " + mUseAddress);
                    mCall.Call(mUseAddress);
                    break;

                case CallEventType.ConnectionFailed:
                    {
                        ErrorEventArgs args = e as ErrorEventArgs;
                        Append("Connection failed error: " + args.Info);
                        InternalResetCall();
                    }
                    break;
                case CallEventType.ConfigurationFailed:
                    {
                        ErrorEventArgs args = e as ErrorEventArgs;
                        Append("Configuration failed error: " + args.Info);
                        InternalResetCall();
                    }
                    break;

                case CallEventType.FrameUpdate:
                    {

                        //new frame received from webrtc (either from local camera or network)
                        if (e is FrameUpdateEventArgs)
                        {
                            UpdateFrame((FrameUpdateEventArgs)e);
                        }
                        break;
                    }

                case CallEventType.Message:
                    {
                        //text message received
                        MessageEventArgs args = e as MessageEventArgs;
                        Append(args.Content);
                        break;
                    }
                case CallEventType.WaitForIncomingCall:
                    {
                        //the chat app will wait for another app to connect via the same string
                        WaitForIncomingCallEventArgs args = e as WaitForIncomingCallEventArgs;
                        Append("Waiting for incoming call address: " + args.Address);
                        break;
                    }
            }

        }

        /// <summary>
        /// Destroys the call. Used if unity destroys the object or if a call
        /// ended / failed due to an error.
        /// 
        /// </summary>
        protected virtual void CleanupCall()
        {
            if (mFrameProcessor != null)
            {
                mFrameProcessor.FreeAll();
            }
            if (mCall != null)
            {
                mCallActive = false;
                mRemoteUserId = ConnectionId.INVALID;
                Debug.Log("Destroying call!");
                mCall.CallEvent -= Call_CallEvent;
                mCall.Dispose();
                mCall = null;
                //call the garbage collector. This isn't needed but helps discovering
                //memory bugs early on.
                Debug.Log("Triggering garbage collection");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Debug.Log("Call destroyed");
            }
        }


        /// <summary>
        /// Create the default configuration for this CallApp instance.
        /// This can be overwritten in a subclass allowing the creation custom apps that
        /// use a slightly different configuration.
        /// </summary>
        /// <returns></returns>
        public virtual MediaConfig CreateMediaConfig()
        {
            MediaConfig mediaConfig = new MediaConfig();
            //testing echo cancellation (native only)
            bool useEchoCancellation = true;
            if (useEchoCancellation)
            {
#if (!UNITY_WEBGL && !UNITY_WSA)
                var nativeConfig = new Byn.Awrtc.Native.NativeMediaConfig();
                nativeConfig.AudioOptions.echo_cancellation = true;

                mediaConfig = nativeConfig;
#endif
            }

#if UNITY_WSA && !UNITY_EDITOR
            var uwpConfig = new Byn.Awrtc.Uwp.UwpMediaConfig();
            uwpConfig.Mrc = true;
            //uwpConfig.ProcessLocalFrames = false;
            //uwpConfig.DefaultCodec = "H264";
            mediaConfig = uwpConfig;
            Debug.Log("Using uwp specific media config: " + mediaConfig);
#endif

            //use video and audio by default (the UI is toggled on by default as well it will change on click )
            mediaConfig.Audio = true;
            mediaConfig.Video = true;
            mediaConfig.VideoDeviceName = null;

            mediaConfig.Format = FramePixelFormat.ABGR;

            mediaConfig.MinWidth = 160;
            mediaConfig.MinHeight = 120;
            //Larger resolutions are possible in theory but
            //allowing users to set this too high is risky.
            //A lot of devices do have great cameras but not
            //so great CPU's which might be unable to
            //encode fast enough.
            mediaConfig.MaxWidth = 1920 * 2;
            mediaConfig.MaxHeight = 1080 * 2;

            //will be overwritten by UI in normal use
            mediaConfig.IdealWidth = 160;
            mediaConfig.IdealHeight = 120;
            mediaConfig.IdealFrameRate = 30;
            return mediaConfig;
        }

        /// <summary>
        /// Destroys the call object and shows the setup screen again.
        /// Called after a call ends or an error occurred.
        /// </summary>
        public virtual void ResetCall()
        {
            //outside quits. don't rejoin automatically
            StopAutoRejoin();
            InternalResetCall();
            mUi.SetGuiState(true);
        }

        private void StopAutoRejoin()
        {
            mAutoRejoin = false;
            if (mAutoRejoinCoroutine != null)
            {
                StopCoroutine(mAutoRejoinCoroutine);
                mAutoRejoinCoroutine = null;
            }
        }

        private void TriggerRejoinTimer()
        {
            Append("Restarting in " + mRejoinTime + " seconds!");
            mAutoRejoinCoroutine = CoroutineRejoin();
            StartCoroutine(mAutoRejoinCoroutine);
        }

        private void InternalResetCall()
        {
            CleanupCall();
            if (mAutoRejoin)
            {
                TriggerRejoinTimer();
            }
        }

        /// <summary>
        /// Allows to control the replay volume of the
        /// remote connection.
        /// </summary>
        /// <param name="volume">
        /// Usually between 0 and 1
        /// </param>
        public virtual void SetRemoteVolume(float volume)
        {
            if (mCall == null)
                return;
            if (mRemoteUserId == ConnectionId.INVALID)
            {
                return;
            }
            mCall.SetVolume(volume, mRemoteUserId);
        }


        /// <summary>
        /// Returns a list of video devices for the UI to show.
        /// This is used to avoid having the UI directly access the UnityCallFactory.
        /// </summary>
        /// <returns></returns>
        public string[] GetVideoDevices()
        {
            if (CanSelectVideoDevice())
            {
                List<string> devices = new List<string>();
                string[] videoDevices = UnityCallFactory.Instance.GetVideoDevices();
                devices.Add("Any");
                devices.AddRange(videoDevices);
                return devices.ToArray();
            }
            else
            {
                return new string[] { "Default" };
            }
        }

        /// <summary>
        /// Used by the UI
        /// </summary>
        /// <returns></returns>
        public bool CanSelectVideoDevice()
        {
            return UnityCallFactory.Instance.CanSelectVideoDevice();
        }

        /// <summary>
        /// Called by UI when the join button is pressed.
        /// </summary>
        /// <param name="address"></param>
        public virtual void Join(string address)
        {
            if (address.Length > MAX_CODE_LENGTH)
                throw new ArgumentException("Address can't be longer than " + MAX_CODE_LENGTH);
            mUseAddress = address;
            InternalJoin();
        }
        private void InternalJoin()
        {
            if (mCallActive)
            {
                Debug.LogError("Join call failed. Call is already/still active");
                return;
            }
            mCallActive = true;
            this.mCall.Listen(mUseAddress);
        }

        private IEnumerator CoroutineRejoin()
        {
            yield return new WaitForSecondsRealtime(mRejoinTime);
            SetupCall();
            InternalJoin();
        }

        /// <summary>
        /// Called by ui to send a message.
        /// </summary>
        /// <param name="msg"></param>
        public virtual void Send(string msg)
        {
            this.mCall.Send(msg);
        }

        /// <summary>
        /// Turns on sending audio for the next call.
        /// </summary>
        /// <param name="value"></param>
        public void SetAudio(bool value)
        {
            mMediaConfig.Audio = value;
        }
        /// <summary>
        /// Turns on sending video for the next call.
        /// </summary>
        /// <param name="value"></param>
        public void SetVideo(bool value)
        {
            mMediaConfig.Video = value;
        }

        /// <summary>
        /// Sets a different format. 
        /// Experimental use only. Most formats only work on specific platforms / specific setups. 
        /// </summary>
        /// <param name="format"></param>
        public virtual void SetFormat(FramePixelFormat format)
        {
            mMediaConfig.Format = format;
        }
        /// <summary>
        /// Allows to set a specific video device.
        /// This isn't supported on WebGL yet.
        /// </summary>
        /// <param name="deviceName"></param>
        public void SetVideoDevice(string deviceName)
        {
            mMediaConfig.VideoDeviceName = deviceName;
        }

        /// <summary>
        /// Changes the target resolution that will be used for
        /// sending video streams.
        /// The closest one the camera can handle will be used.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetIdealResolution(int width, int height)
        {
            mMediaConfig.IdealWidth = width;
            mMediaConfig.IdealHeight = height;
        }

        /// <summary>
        /// Sets the ideal FPS.
        /// This has a lower priority than the ideal resolution.
        /// Note that the FPS aren't enforced. It pick
        /// the closest FPS the video device supports.
        /// </summary>
        /// <param name="fps"></param>
        public void SetIdealFps(int fps)
        {
            mMediaConfig.IdealFrameRate = fps;
        }

        /// <summary>
        /// True will show the local video.
        /// False will not return the video and thus
        /// save some CPU work.
        /// </summary>
        /// <param name="showLocalVideo"></param>
        public void SetShowLocalVideo(bool showLocalVideo)
        {
            mLocalFrameEvents = showLocalVideo;
        }

        /// <summary>
        /// Can be used to make the app automatically reconnect
        /// if a sudden disconnect occurred or the other side ends
        /// the connection.
        /// </summary>
        /// <param name="rejoin"></param>
        /// <param name="rejoinTime"></param>
        public void SetAutoRejoin(bool rejoin, float rejoinTime = 4)
        {
            mAutoRejoin = rejoin;
            mRejoinTime = rejoinTime;
        }

        /// <summary>
        /// Forwarded to the call factory.
        /// Returns the loudspeaker status on mobile devices.
        /// 
        /// </summary>
        /// <returns></returns>
        public bool GetLoudspeakerStatus()
        {
            //check if call is created to ensure this isn't called before initialization
            if (mCall != null)
            {
                return UnityCallFactory.Instance.GetLoudspeakerStatus();
            }
            return false;
        }

        /// <summary>
        /// Sets the loudspeaker mode via the call factory.
        /// </summary>
        /// <param name="state"></param>
        public void SetLoudspeakerStatus(bool state)
        {
            //check if call is created to ensure this isn't called before initialization
            if (mCall != null)
            {
                UnityCallFactory.Instance.SetLoudspeakerStatus(state);
            }
        }

        /// <summary>
        /// Set to true to mute the microphone.
        /// </summary>
        /// <param name="state"></param>
        public void SetMute(bool state)
        {
            //check if call is created to ensure this isn't called before initialization
            if (mCall != null)
            {
                mCall.SetMute(state);
            }
        }

        /// <summary>
        /// True if the microphone is muted (or sending audio isn't active).
        /// </summary>
        /// <returns></returns>
        public bool IsMute()
        {
            //check if call is created to ensure this isn't called before initialization
            if (mCall != null)
            {
                return mCall.IsMute();
            }
            return true;
        }

        protected virtual void UpdateFrame(FrameUpdateEventArgs frameUpdateEventArgs)
        {
            //Debug.Log("frame update remote: " + frameUpdateEventArgs.IsRemote);
            mFrameProcessor.Process(frameUpdateEventArgs);
        }

        protected virtual void OnFrameProcessed(ProcessedFrame frame)
        {
            if (frame.MetaData.IsRemote == false)
            {
                mUi.UpdateLocalTexture(frame);
            }
            else
            {
                mUi.UpdateRemoteTexture(frame);
            }
        }
        protected virtual void Append(string txt)
        {
            mUi.Append(txt);
        }
    }
}