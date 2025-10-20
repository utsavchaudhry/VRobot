/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using Byn.Awrtc;
using Byn.Awrtc.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.UI;
using System.IO;

namespace Byn.Unity.Examples
{

    /// <summary>
    /// USE AT YOUR OWN RISK!
    /// 
    /// Allows to test the future feature for conference calls / n to n connections.
    /// 
    /// The current conference support is still limited. It will be replaced with a better
    /// implementation in the future. If you need a more stable (but more complicated)
    /// method now use IMediaNetwork instead.
    /// 
    /// Typical problems with the ConferenceApp:
    /// * handling multiple streams can be very CPU intensive (keep resolution low)
    /// * System can't handle failed direct connections
    ///     e.g. if 3 users join the same call but two of them 
    ///     just can't connect directly due to firewall / stun fails.
    ///     Use TURN if possible to reduce the risk of this happening!
    /// 
    /// 
    /// Note the signaling server server needs the correct flag in config.json
    ///     "address_sharing": true
    ///     !!!!
    ///     
    /// e.g.:
    /// 
    ///    "apps": [
    ///        {
    ///            "name": "ConferenceApp",
    ///            "path": "/conferenceapp",
    ///            "address_sharing": true
    ///        }
    ///        
    /// for url ws://because-why-not.com:12776/conferenceapp
    /// </summary>
    public class ConferenceApp : MonoBehaviour
    {
        /// <summary>
        /// Length limit of signaling server address
        /// </summary>
        private const int MAX_CODE_LENGTH = 256;

        #region UI
        /// <summary>
        /// Input field used to enter the room name.
        /// </summary>
        public InputField uRoomName;

        /// <summary>
        /// Input field to enter a new message.
        /// </summary>
        public InputField uMessageField;

        /// <summary>
        /// Output message list to show incoming and sent messages + output messages of the
        /// system itself.
        /// </summary>
        public MessageList uOutput;

        /// <summary>
        /// Join button to connect to a server.
        /// </summary>
        public Button uJoin;

        /// <summary>
        /// Send button.
        /// </summary>
        public Button uSend;


        /// <summary>
        /// Shutdown button. Disconnects all connections + shuts down the server if started.
        /// </summary>
        public Button uShutdown;

        /// <summary>
        /// Panel with the join button. Will be hidden after setup
        /// </summary>
        public GameObject uSetupPanel;

        /// <summary>
        /// Space used for video images
        /// </summary>
        public GameObject uVideoLayout, uSelfVideoLayout;

        /// <summary>
        /// Prefab used for new user screen / video image
        /// </summary>
        public GameObject uVideoPrefab;


        /// <summary>
        /// Texture used to indicate users that don't stream video.
        /// </summary>
        public Texture2D uNoImgTexture;
        #endregion

        /// <summary>
        /// Call class handling all the functionality
        /// </summary>
        private ICall mCall;

        private MediaConfig mMediaConfig = new MediaConfig();
        /// <summary>
        /// Configuration of audio / video functionality
        /// </summary>
        public MediaConfig MediaConfig
        {
            get
            {
                return mMediaConfig;
            }
            set
            {

                mMediaConfig = value;
            }
        }

        private NetworkConfig mNetConfig = new NetworkConfig();
        /// <summary>
        /// Network / server configuration
        /// </summary>
        public NetworkConfig NetConfig { get { return mNetConfig; } set { mNetConfig = value; } }

        /// <summary>
        /// Class used to keep track of each individual connection and its data / ui
        /// </summary>
        private class VideoData
        {
            public GameObject uiObject;
            public Texture2D texture;
            public RawImage image;
        }

        /// <summary>
        /// Dictionary to resolve connection ID with their specific data
        /// </summary>
        private Dictionary<ConnectionId, VideoData> mVideoUiElements = new Dictionary<ConnectionId, VideoData>();

        public int ConnectionCount
        {
            get
            {
                return mVideoUiElements.Count - 1;
            }
        }

        //We create this randomly for now in Start(). It could be entered by the user via the UI
        private string mOwnUserName = "User";

        Dictionary<ConnectionId, string> mIdToUser;

        /// <summary>
        /// Unity start.
        /// </summary>
       //Robot controller variables ------------------------------
        public bool mIsHost;
        private ConnectionId? mCurrentClient = null; // Currently controlling client (null if none)
        private Queue<ConnectionId> mClientQueue = new Queue<ConnectionId>(); // Waiting clients
        private float mControlTimer = 0f;
        [SerializeField] private float mControlTimeLimit = 1200f;
        [SerializeField] private float baseQueueWaitTime = 1200f; // total wait time in queue
        private float myWaitTime = 0f;
        private bool isIdAssigned;
        private bool mIsActiveClient = false;
        private const float MUTED_VOLUME = 0.0f;
        private const float UNMUTED_VOLUME = 1.0f;
        private bool timerStarted, isTimeoverFlag;

        private List<ConnectionId> mConnectedClients = new List<ConnectionId>();

        [Header("Robot Control UI")]
        public Text uCurrentTimeText;
        public Text uCurrentControllerText;
        public Text uQueueCountText;
        public Text uQueueListText;
        public Text uTimeRemainingText;
        public Text uQueueTimeText;
        public Text uUserNameText;
        private ConnectionId? mHostConnectionId = null;
        private string mIdRequestToken;
        private ConnectionId mMyConnectionId, mRearId;
        private ConnectionId lastDisconnectId;

        public static event Action OnUserChanged, OnUserDisconnected;

        private Coroutine timerRoutine;
        public Texture2D clientVideoTexture, HostVideoTexture;

        //Host Availability variables;
        private float mLastHostPingTime;
        private const float HOST_PING_INTERVAL = 5f; // Send ping every 3 seconds
        private const float HOST_TIMEOUT_DURATION = 15f; // Consider host dead after 15 seconds
        public bool mIsHostAvailable = false;
        private Coroutine mHeartbeatCoroutine;

        private void Start()
        {
            UnityCallFactory.RequestLogLevelStatic(UnityCallFactory.LogLevel.Info);
            UnityCallFactory.EnsureInit(OnCallFactoryReady, OnCallFactoryFailed);
            //lets just give them a random number for now. 
            mOwnUserName = mOwnUserName + "_" + (int)UnityEngine.Random.Range(0, 10000);
            mIdToUser = new Dictionary<ConnectionId, string>();

            _ = StartCoroutine(JoinWithDelay());
            //if (!mIsHost)
            //{
            //    mHeartbeatCoroutine = StartCoroutine(PingSystem());
            //}
        }

        //private IEnumerator PingSystem()
        //{
        //    while (mCall != null && !mIsHost)
        //    {
        //        yield return new WaitForSeconds(HOST_PING_INTERVAL);

        //        // Send ping to host if we know the host ID
        //        if (mHostConnectionId.HasValue)
        //        {
        //            mCall.Send("PING", true, mHostConnectionId.Value);

        //            // Check if host hasn't responded in timeout duration
        //            if (Time.time - mLastHostPingTime > HOST_TIMEOUT_DURATION && mIsHostAvailable)
        //            {
        //                OnHostDisconnected();
        //            }
        //        }
        //    }
        //}

        //private void HandleHeartbeatMessages(MessageEventArgs args)
        //{
        //    if (args.Content == "PING")
        //    {
        //        if (mIsHost)
        //        {
        //            // Host responds to ping
        //            mCall.Send("PONG", true, args.ConnectionId);
        //        }
        //    }
        //    else if (args.Content == "PONG")
        //    {
        //        if (!mIsHost && args.ConnectionId == mHostConnectionId)
        //        {
        //            // Client received response from host
        //            mLastHostPingTime = Time.time;

        //        }
        //    }
        //}

        private void OnHostDisconnected() //handle host disconenction events here
        {
            mIsHostAvailable = false;
            Append("Warning: Host appears to be disconnected!");

            if (mIsActiveClient)
            {
                mIsActiveClient = false;
            }
        }

        private IEnumerator JoinWithDelay()
        {
            yield return new WaitForSeconds(0.5f);

            AudioToggle(true);
            VideoToggle(true);

            JoinButtonPressed();
        }

        [Space]

        [SerializeField] private string defaultRoomName = "ShopMetal_1";

        protected virtual void OnCallFactoryReady()
        {
            //to trigger android permission requests
            StartCoroutine(ExampleGlobals.RequestPermissions());

            MediaConfig.Video = true;
            MediaConfig.Audio = true;
            MediaConfig.VideoDeviceName = UnityCallFactory.Instance.GetDefaultVideoDevice();

            NetConfig.KeepSignalingAlive = true;
            NetConfig.MaxIceRestart = 5;
            NetConfig.IceServers.Add(ExampleGlobals.DefaultIceServer);
            NetConfig.SignalingUrl = ExampleGlobals.SignalingConference;
            NetConfig.IsConference = true;
            uRoomName.text = "ShopMetal";
        }

        public int GetClientCount()
        {
            return mClientQueue.Count;
        }

        protected virtual void OnCallFactoryFailed(string error)
        {
            string fullErrorMsg = typeof(CallApp).Name + " can't start. The " + typeof(UnityCallFactory).Name + " failed to initialize with following error: " + error;
            Debug.LogError(fullErrorMsg);
        }


        /// <summary>
        /// Creates the call object and uses the configure method to activate the 
        /// video / audio support if the values are set to true.
        /// </summary>
        /// <param name="useAudio">Uses the local microphone for the call</param>
        /// <param name="useVideo">Uses a local camera for the call. The camera will start
        /// generating new frames after this call so the user can see himself before
        /// the call is connected.</param>
        private void Setup(bool useAudio = false, bool useVideo = false)
        {
            Append("Setting up ...");

            //setup the server
            Debug.Log("Creating ICall with " + NetConfig);
            mCall = UnityCallFactory.Instance.Create(NetConfig);
            if (mCall == null)
            {
                Append("Failed to create the call");
                return;
            }

            Append("Call created!");
            mCall.CallEvent += Call_CallEvent;

            //setup local video element
            mCall.Configure(MediaConfig);

            SetGuiState(false);
        }

        /// <summary>
        /// Destroys the call object and shows the setup screen again.
        /// Called after a call ends or an error occurred.
        /// </summary>
        private void ResetCall()
        {
            foreach (var v in mVideoUiElements)
            {
                Destroy(v.Value.uiObject);
                if (v.Value.texture != null)
                    Destroy(v.Value.texture);
            }
            mVideoUiElements.Clear();
            CleanupCall();
            SetGuiState(true);
        }

        /// <summary>
        /// Handler of call events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Call_CallEvent(object sender, CallEventArgs e)
        {
            switch (e.Type)
            {
                case CallEventType.CallAccepted:
                    OnNewCall(e as CallAcceptedEventArgs);
                    break;

                case CallEventType.CallEnded:
                    OnCallEnded(e as CallEndedEventArgs);
                    break;

                case CallEventType.ListeningFailed:
                    Append("Failed to listen for incoming calls! Server might be down!");
                    ResetCall();
                    break;

                case CallEventType.ConnectionFailed:
                    {
                        Awrtc.ErrorEventArgs args = e as Awrtc.ErrorEventArgs;
                        Append("Error: " + args.Info);
                        Debug.LogError(args.Info);
                        ResetCall();
                        break;
                    }

                case CallEventType.FrameUpdate:
                    FrameUpdateEventArgs frameargs = e as FrameUpdateEventArgs;
                    UpdateFrame(frameargs);
                    break;

                case CallEventType.Message:
                    {
                        MessageEventArgs args = e as MessageEventArgs;

                        if (!mIdToUser.ContainsKey(args.ConnectionId))
                        {
                            AddNewConnection(args.ConnectionId);
                        }

                        bool isSystemMessage = args.Content.StartsWith("REQUEST_ID:") ||
                         args.Content.StartsWith("YOUR_ID:") ||
                         args.Content.StartsWith("REAR:") ||
                         args.Content.StartsWith("CONTROL_GRANTED:") ||
                         args.Content.StartsWith("WAIT_TIME:") ||
                         args.Content.StartsWith("DISCONNECTED:") ||
                         args.Content.StartsWith("EXTEND_TIME:") ||
                         args.Content.StartsWith("SWITCH_CONTROL") ||
                         args.Content.StartsWith("CLIENT:") ||
                         args.Content.StartsWith("HOST:");

                        if (!isSystemMessage)
                        {
                            if (mIsHost)
                            {
                                if (mCurrentClient == null || args.ConnectionId != mCurrentClient.Value)
                                {
                                    mCall.Send("MESSAGE_BLOCKED:You are not the active controller", true, args.ConnectionId);
                                    Debug.Log($"[BLOCKED] Host rejected msg from non-active client {args.ConnectionId}: {args.Content}");
                                    return;
                                }
                            }
                            else
                            {
                                if (mHostConnectionId == null || args.ConnectionId != mHostConnectionId.Value)
                                {
                                    Debug.Log($"[BLOCKED] Client rejected msg from non-host {args.ConnectionId}: {args.Content}");
                                    return;
                                }
                            }
                        }

                        if (mIdToUser[args.ConnectionId] == "unknown")
                        {
                            string name = args.Content;
                            OnNewUserDiscovered(name, args.ConnectionId);
                        }
                        else
                        {
                            string name = mIdToUser[args.ConnectionId];
                            Append(name + ":" + args.Content);
                        }

                        if (args.Content.StartsWith("REQUEST_ID:"))
                        {
                            if (mIsHost)
                            {
                                string token = args.Content.Substring(11);
                                mCall.Send($"YOUR_ID:{token}:{args.ConnectionId}");
                                uUserNameText.text = $" connection ID: {mMyConnectionId}";
                                Debug.Log($"[ID_SEND] Sent YOUR_ID:{token}:{args.ConnectionId} to {args.ConnectionId}");
                            }
                        }

                        if (args.Content.StartsWith("YOUR_ID:"))
                        {
                            if (!mIsHost && !isIdAssigned)
                            {
                                string[] parts = args.Content.Split(':');
                                mMyConnectionId = new ConnectionId(short.Parse(parts[2]));
                                uUserNameText.text = $" connection ID: {mMyConnectionId}";
                                isIdAssigned = true;

                                Debug.Log($"[ID_RECEIVE] Assigned self connection ID: {mMyConnectionId}");
                            }
                        }

                        if (args.Content.StartsWith("REAR:"))
                        {
                            string rearInQueue = args.Content.Substring(5);
                            mRearId = new ConnectionId(short.Parse(rearInQueue));
                            Debug.Log($"[QUEUE] Rear of queue updated: {mRearId}");

                            if (isTimeoverFlag)
                            {
                                isTimeoverFlag = false;
                                mCall.Send($"SWITCH_CONTROL:{mMyConnectionId}");
                                RemoveVideo(mHostConnectionId.Value);
                                Debug.Log($"[TIMEOVER] Time expired. Sent SWITCH_CONTROL:{mMyConnectionId}");
                            }
                        }

                        if (args.Content.StartsWith("CONTROL_GRANTED:"))
                        {
                            string _id = args.Content.Substring(16);
                            if (short.TryParse(_id, out short connId))
                            {
                                ConnectionId roleAssignedId = new ConnectionId(connId);
                                Debug.Log($"[CONTROL_GRANTED_RECEIVE] Received CONTROL_GRANTED:{roleAssignedId}. My ID: {mMyConnectionId}");


                                Append("Control granted to you! You can now control the robot.");

                                if (mHostConnectionId.HasValue)
                                {
                                    mCall.SetVolume(UNMUTED_VOLUME, mHostConnectionId.Value);
                                }

                                mIsActiveClient = true;

                                if (!mIsHost)
                                {
                                    StartCoroutine(HandleRobotCommand());
                                }

                            }
                        }

                        if (args.Content.StartsWith("WAIT_TIME:"))
                        {
                            string[] parts = args.Content.Split(':');
                            myWaitTime = float.Parse(parts[1]);
                            Debug.Log($"[WAIT_TIME] My wait time is {myWaitTime}");

                            if (myWaitTime < 0)
                            {
                                uQueueTimeText.text = "Not in queue";
                            }

                            if (!timerStarted)
                            {
                                timerStarted = true;
                                timerRoutine = StartCoroutine(SetWaitTime(myWaitTime));
                                Debug.Log("[WAIT_TIME] Started SetWaitTime coroutine.");
                            }
                        }

                        if (args.Content.StartsWith("CONTROL_REVOKED:"))
                        {
                            if (!mIsHost)
                            {
                                mIsActiveClient = false;

                                if (mHostConnectionId.HasValue)
                                {
                                    mCall.SetVolume(MUTED_VOLUME, mHostConnectionId.Value);
                                }

                                Append("Control has been revoked - you can no longer send messages or speak");
                                Debug.Log("[CONTROL_REVOKED] Control revoked on client.");
                            }
                        }

                        if (args.Content.StartsWith("DISCONNECTED:"))
                        {
                            float discardedQueueTime = float.Parse(args.Content.Substring(13));
                            Debug.Log($"[DISCONNECTED] Lost user. Decreasing queue time by {discardedQueueTime}");
                            DecreaseQueueTime(discardedQueueTime, lastDisconnectId);
                        }

                        if (args.Content.StartsWith("EXTEND_TIME:"))
                        {
                            float extraTime = float.Parse(args.Content.Substring(12));
                            Debug.Log($"[EXTEND_TIME] Received {extraTime}s extension.");
                            myWaitTime -= extraTime;

                            StopCoroutine(timerRoutine);
                            timerRoutine = StartCoroutine(SetWaitTime(myWaitTime));
                        }

                        if (args.Content.StartsWith("SWITCH_CONTROL"))
                        {
                            if (mIsHost)
                            {
                                AssignNextClient();
                            }
                        }

                        break;
                    }

                case CallEventType.WaitForIncomingCall:
                    {
                        WaitForIncomingCallEventArgs args = e as WaitForIncomingCallEventArgs;
                        Append("Waiting for incoming call address: " + args.Address);
                        break;
                    }
            }
        }


        /// <summary>
        /// Event triggers for a new incoming call
        /// (in conference mode there is no difference between incoming / outgoing)
        /// </summary>
        /// <param name="args"></param>
        private void OnNewCall(CallAcceptedEventArgs args)
        {
            AddNewConnection(args.ConnectionId);

            if (mIsHost)
            {
                mCall.Send("HOST:" + mOwnUserName);
                Append("Robot is ready. Waiting for clients...");
                SetupVideoUi(ConnectionId.INVALID, uSelfVideoLayout);
            }
            else
            {
                mCall.Send("CLIENT:" + mOwnUserName);
                if (!isIdAssigned)
                {
                    mIdRequestToken = Guid.NewGuid().ToString().Substring(0, 8);
                    mCall.Send($"REQUEST_ID:{mIdRequestToken}");
                    Debug.Log("Requested our connection ID from host");
                }

                if (mHostConnectionId.HasValue && !mIsActiveClient)
                {
                    mCall.SetVolume(MUTED_VOLUME, mHostConnectionId.Value);
                }
            }

        }

        /// <summary>
        /// Adds a new user. Can be called several times without adding the user twice
        /// </summary>
        /// <param name="id">
        /// ConnectionId the new user
        /// </param>
        private void AddNewConnection(ConnectionId id)
        {
            //new connection. we do not know who that is yet until we get the first message!
            if (mIdToUser.ContainsKey(id) == false)
            {
                mIdToUser[id] = "unknown";
                Append("New connection with ID " + id);
            }

        }

        private void OnNewUserDiscovered(string name, ConnectionId id)
        {
            Debug.Log("Received first message from ConnectionId " + id + "! Their username is " + name);


            if (name.StartsWith("CLIENT:")) //host reads this
            {
                mIdToUser[id] = name.Substring(7);
                Append("Client connected: " + mIdToUser[id]);
                mConnectedClients.Add(id);

                if (mIsHost)
                {
                    // Add client to queue
                    mClientQueue.Enqueue(id);
                    Append($"{id} Added To Queue");

                    ConnectionId lastId = mClientQueue.Last();
                    mCall.Send("REAR:" + lastId);

                    UpdateClientWaitTimes();

                    if (mCurrentClient == null && mClientQueue.Count == 1)   // If this is the first client and no one is currently controlling, start immediately
                    {
                        StartCoroutine(AssignFirstClientAfterDelay());
                    }
                }
            }

            if (name.StartsWith("HOST:")) // client reads this
            {
                mIdToUser[id] = name.Substring(5);
                Append("HOST connected: " + mIdToUser[id]);
                mHostConnectionId = id;
                mIsHostAvailable = true;
                SetupVideoUi(id, uVideoLayout);
                if (!mIsActiveClient)
                {
                    mCall.SetVolume(MUTED_VOLUME, id);
                }
            }


            UpdateQueueUI();
        }

        private IEnumerator AssignFirstClientAfterDelay()
        {
            // Small delay to ensure client is fully connected
            yield return new WaitForSeconds(3f);

            if (mClientQueue.Count > 0 && mCurrentClient == null)
            {
                ConnectionId candidate = mClientQueue.Peek();
                AssignNextClient();
            }
        }

        public bool IsActiveClient()
        {
            return mIsActiveClient;
        }

        private void AssignNextClient()
        {
            if (mClientQueue.Count == 0)
            {
                // mCurrentClient = null;
                Append("No clients in queue.");

                i = 0;
                return;
            }

            if (mCurrentClient.HasValue)
            {
                // Mute the previous client if there was one
                mCall.SetVolume(MUTED_VOLUME, mCurrentClient.Value);
                RemoveVideo(mCurrentClient.Value);

                mCall.Send("CONTROL_REVOKED:Control transferred to next client", true, mCurrentClient.Value);
            }


            // Dequeue next client
            ConnectionId nextClient = mClientQueue.Dequeue();
            mCurrentClient = nextClient;
            mControlTimer = 0f;

            SetupVideoUi(mCurrentClient.Value, uVideoLayout);
            mCall.Send($"CONTROL_GRANTED:{nextClient}");
            mCall.SetVolume(UNMUTED_VOLUME, mCurrentClient.Value);

            // UpdateClientWaitTimes();
            UpdateQueueUI();

            OnUserChanged?.Invoke();
        }

        int i = 0;
        private void UpdateClientWaitTimes()
        {
            if (!mIsHost) return;

            if (mClientQueue.Count == 0)
            {
                i = 0;
            }

            float currentWaitTime = i * baseQueueWaitTime;

            if (mClientQueue.Count != 0)
            {
                ConnectionId lastClient = mClientQueue.Last();
                mCall.Send($"WAIT_TIME:{currentWaitTime}", true, lastClient);

                i++;
            }

        }
        void RemoveVideo(ConnectionId _id)
        {
            VideoData data;
            if (mVideoUiElements.TryGetValue(_id, out data))
            {
                if (data.texture != null)
                    Destroy(data.texture);
                Destroy(data.uiObject);
                mVideoUiElements.Remove(_id);
            }
        }

        private string FormatTimeMmSs(float timeSeconds)
        {
            //if (timeSeconds < 0)
            //    throw new ArgumentOutOfRangeException(nameof(timeSeconds), "Time must be non-negative.");
            if (timeSeconds < 0)
            {
                return "Timer Over";
            }

            int totalSeconds = Mathf.FloorToInt(timeSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        private IEnumerator HandleRobotCommand()
        {
            float lastMsgTime = 0f;

            if (!mIsHost)
            {

                while (mControlTimer < mControlTimeLimit)
                {
                    mControlTimer += Time.deltaTime;

                    if (mControlTimer - lastMsgTime >= 1f)
                    {
                        //SendMsg($"Sending command to {_id}");
                        lastMsgTime = mControlTimer;
                    }

                    uTimeRemainingText.text = "Time Remaining : " + FormatTimeMmSs(mControlTimeLimit - mControlTimer);
                    yield return null;
                }

                isTimeoverFlag = true;

                if (mMyConnectionId != mRearId)
                {
                    mCall.Send($"SWITCH_CONTROL:{mMyConnectionId}");
                }
            }
        }


        float waitTimer = 0f;
        private IEnumerator SetWaitTime(float baseTime)
        {
            float lastMsgTime = 0f;
            if (!mIsHost)
            {
                while (waitTimer <= baseTime)
                {
                    waitTimer += Time.deltaTime;

                    if (waitTimer - lastMsgTime >= 1f)
                    {
                        uQueueTimeText.text = "Please Wait " + FormatTimeMmSs(baseTime - waitTimer);
                        lastMsgTime = waitTimer;
                    }

                    if (mIsActiveClient)
                    {
                        uQueueTimeText.enabled = false;
                        yield break;
                    }

                    yield return null;
                }
            }
        }
        private void OnUserLeft(ConnectionId id)
        {
            if (mIdToUser.ContainsKey(id))
            {
                string name = mIdToUser[id];
                Append("User with name " + name + "got disconnected");
            }
        }

        /// <summary>
        /// Creates the connection specific data / ui
        /// </summary>
        /// <param name="id"></param>
        private void SetupVideoUi(ConnectionId id, GameObject parentobj)
        {
            //create texture + ui element
            VideoData vd = new VideoData();
            vd.uiObject = Instantiate(uVideoPrefab);
            vd.uiObject.transform.SetParent(parentobj.transform, false);

            vd.image = vd.uiObject.GetComponentInChildren<RawImage>();
            vd.image.texture = uNoImgTexture;
            mVideoUiElements[id] = vd;
        }

        /// <summary>
        /// User left. Cleanup connection specific data / ui
        /// </summary>
        /// <param name="args"></param>
        private void OnCallEnded(CallEndedEventArgs args)
        {
            // Clients tell the host they’re leaving
            if (!mIsHost && mHostConnectionId.HasValue)
            {
                mCall.Send("DISCONNECTED", true, mHostConnectionId.Value);
            }

            lastDisconnectId = args.ConnectionId;

            if (mIsHost)
            {
                // Remove from connected clients
                if (mConnectedClients.Contains(args.ConnectionId))
                {
                    mConnectedClients.Remove(args.ConnectionId);
                }

                // If the one who left was the current controller,
                // immediately give control to the newest remaining client
                if (mCurrentClient.HasValue && mCurrentClient.Value == args.ConnectionId)
                {
                    mCurrentClient = null;

                    if (mConnectedClients.Count > 0)
                    {
                        // Pick the *last* client in the list
                        ConnectionId next = mConnectedClients.Last();
                        mCurrentClient = next;

                        SetupVideoUi(mCurrentClient.Value, uVideoLayout);
                        mCall.Send($"CONTROL_GRANTED:{next.id}");
                        mCall.SetVolume(UNMUTED_VOLUME, mCurrentClient.Value);
                    }
                }
            }

            RemoveVideo(args.ConnectionId);
            OnUserLeft(args.ConnectionId);
            OnUserDisconnected?.Invoke();
        }


        void DecreaseQueueTime(float seconds, ConnectionId disconnectClientId)
        {
            List<ConnectionId> connectionIdList = mClientQueue.ToList();
            int index = connectionIdList.IndexOf(disconnectClientId); //disconnectedid is already kickedoutfrom queue

            if (index >= 0)
            {
                List<ConnectionId> remainingNextId = connectionIdList.Skip(index + 1).ToList();

                foreach (var id in remainingNextId)
                {
                    mCall.Send($"EXTEND_TIME:{seconds}", true, id);
                }
            }

            if (mClientQueue.Contains(disconnectClientId))
            {
                lastDisconnectId = disconnectClientId;
                var newQueue = new Queue<ConnectionId>(mClientQueue.Where(id => id != disconnectClientId)); //remove the disconencted client from clientQueue and make new queue
                mClientQueue = newQueue;
            }

            // If current client disconnected, assign next
            if (mCurrentClient.HasValue && mCurrentClient.Value == disconnectClientId)
            {
                mCurrentClient = null; // Clear current client 
                AssignNextClient();
            }
            UpdateQueueUI();
        }

        private void UpdateQueueUI()
        {
            if (!mIsHost) return;

            if (mCurrentClient != null && mIdToUser.ContainsKey(mCurrentClient.Value))
            {
                uCurrentControllerText.text = $"Current controller: {mIdToUser[mCurrentClient.Value]}";
            }
            else
            {
                uCurrentControllerText.text = "Current controller: None";
            }

            uQueueCountText.text = $"Clients in queue: {mClientQueue.Count}";

            // Update queue list (reorder incase of/after disconnection)
            string queueList = "";
            int position = 1;
            foreach (var clientId in mClientQueue)
            {
                if (mIdToUser.ContainsKey(clientId))
                {
                    queueList += $"{position}. {mIdToUser[clientId]}\n";
                    position++;
                }
            }
            uQueueListText.text = queueList;
        }

        /// <summary>
        /// Updates the frame for a connection id. If the id is new it will create a
        /// visible image for it. The frame can be null for connections that
        /// don't sent frames.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="frame"></param>
        private void UpdateFrame(FrameUpdateEventArgs args)
        {
            if (mVideoUiElements.ContainsKey(args.ConnectionId))
            {
                VideoData videoData = mVideoUiElements[args.ConnectionId];
                //make sure not to overwrite / destroy our texture for missing image data
                if (videoData.image.texture == this.uNoImgTexture)
                    videoData.image.texture = null;
                bool mirror = args.IsRemote == false;
                //converts the frame data to a texture and sets it to the raw image
                UnityMediaHelper.UpdateRawImageTransform(videoData.image, args.Frame, mirror);
                videoData.texture = videoData.image.texture as Texture2D;

                if (mIsHost)
                {
                    if (args.IsRemote) clientVideoTexture = videoData.texture;
                    else HostVideoTexture = videoData.texture;
                }
            }
        }

        /// <summary>
        /// Destroys the call. Used if unity destroys the object or if a call
        /// ended / failed due to an error.
        /// 
        /// </summary>
        private void CleanupCall()
        {
            if (mCall != null)
            {

                Debug.Log("Destroying call!");
                mCall.Dispose();
                mCall = null;
                Debug.Log("Call destroyed");
            }
        }
        private void OnDestroy()
        {
            CleanupCall();
        }


        /// <summary>
        /// toggle audio on / off
        /// </summary>
        /// <param name="state"></param>
        public void AudioToggle(bool state)
        {
            MediaConfig.Audio = state;
        }

        /// <summary>
        /// toggle video on / off
        /// </summary>
        /// <param name="state"></param>
        public void VideoToggle(bool state)
        {
            MediaConfig.Video = state;
        }

        public static event Action<string> OnMsgReceived;

        /// <summary>
        /// Adds a new message to the message view
        /// </summary>
        /// <param name="text"></param>
        private void Append(string text)
        {
            OnMsgReceived?.Invoke(text);

            if (uOutput != null)
            {
                uOutput.AddTextEntry(text);
            }
            else
            {
                Debug.Log("Chat: " + text);
            }
        }

        private float assignClientFreezeTimer;

        /// <summary>
        /// The call object needs to be updated regularly to sync data received via webrtc with
        /// unity. All events will be triggered during the update method in the unity main thread
        /// to avoid multi threading errors
        /// </summary>
        private void Update()
        {
            if (mCall != null)
            {
                // Always tick the WebRTC call so events get processed
                mCall.Update();

                // Host no longer force-assigns here.
                // Assignment is handled explicitly via AssignFirstClientAfterDelay()
                // after a client handshake.
            }
        }

        #region UI 
        /// <summary>
        /// Shows the setup screen or the chat + video
        /// </summary>
        /// <param name="showSetup">true Shows the setup. False hides it.</param>
        private void SetGuiState(bool showSetup)
        {
            uSetupPanel.SetActive(showSetup);

            uSend.interactable = !showSetup;
            uShutdown.interactable = !showSetup;
            uMessageField.interactable = !showSetup;

        }

        /// <summary>
        /// Join button pressed. Tries to join a room.
        /// </summary>
        public void JoinButtonPressed()
        {
            Setup();
            EnsureLength();
            mCall.Listen(uRoomName.text);
            //setup host (first joiner is host)

        }

        /// <summary>
        /// Helper to enforce the length limit
        /// </summary>
        private void EnsureLength()
        {
            if (uRoomName.text.Length > MAX_CODE_LENGTH)
            {
                uRoomName.text = uRoomName.text.Substring(0, MAX_CODE_LENGTH);
            }
        }

        /// <summary>
        /// This is called if the send button
        /// </summary>
        public void SendButtonPressed()
        {
            //get the message written into the text field
            string msg = uMessageField.text;
            SendMsg(msg);
        }

        /// <summary>
        /// User either pressed enter or left the text field
        /// -> if return key was pressed send the message
        /// </summary>
        public void InputOnEndEdit()
        {
            if (Input.GetKey(KeyCode.Return))
            {
                string msg = uMessageField.text;
                SendMsg(msg);
            }
        }

        /// <summary>
        /// Sends a message to the other end
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(string msg)
        {
            if (String.IsNullOrEmpty(msg))
            {
                //never send null or empty messages. webrtc can't deal with that
                return;
            }

            // Check if client is allowed to send messages
            if (!mIsHost && !mIsActiveClient)
            {
                Append("Message blocked - You are not the active controller");
                uMessageField.text = "";
                uMessageField.Select();
                return;
            }

            Append(msg);
            mCall.Send(msg);

            //reset UI
            uMessageField.text = "";
            uMessageField.Select();
        }



        /// <summary>
        /// Shutdown button pressed. Shuts the network down.
        /// </summary>
        public void ShutdownButtonPressed()
        {
            ResetCall();
        }
        #endregion
    }

}