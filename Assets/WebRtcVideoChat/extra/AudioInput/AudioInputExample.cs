using Byn.Awrtc;
using Byn.Awrtc.Native;
using Byn.Awrtc.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// This keeps track of the Factory instance and AudioInput and fills in some gaps in functionality of the
    /// current C++ wrapper version.
    /// 
    /// Tasks:
    /// * Keep track of instances that depend on the factory. If the factory is destroyed all dependencies must be
    /// destroyed. 
    /// * Initialize dependencies when the factory is ready
    /// * allow to switch between shared instance with UnityCallFactory or unique instance (see USE_UNIQUE_FACTORY)
    /// * gives the apps a way to notify an audio device when it is used
    /// 
    /// The UnityCallFactory will take over these tasks in the future.
    /// </summary>
    public class AudioInputExample : MonoBehaviour
    {
        /// <summary>
        /// true - We use an isolated factory from other examples. This allows multiple apps to run in parallel
        /// without interfering. e.g. virtual audio/input devices will only show up in our example but not others.
        /// Also for audio input we have to disable hardware microphone access which could break other examples.
        /// 
        /// false - use the UnityCallFactory at risk of interfering with other apps
        /// </summary>
        public static readonly bool USE_UNIQUE_FACTORY = true;


        private bool mIsReady = false;

        private static AudioInputExample instance;
        public static AudioInputExample Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("AudioInputExample accessed before the instance could be initialized.");
                    return null;
                }
                return instance;
            }
        }

        private NativeAwrtcFactory mFactory;
        /// <summary>
        /// Returns the factory instance used by this example. All devices that generate audio
        /// must use the same instance as this CallApp.
        /// </summary>
        public NativeAwrtcFactory Factory { get { return mFactory; } }

        private WebRtcCSharp.AudioInput mInput;
        /// <summary>
        /// Returns the AudioInput instance used. All devices that generate audio
        /// must use the same instance as this CallApp.
        /// </summary>
        public WebRtcCSharp.AudioInput AudioInput { get { return mInput; } }

        /// <summary>
        /// List of scripts that depend on AudioInput either using IAudioInputDependency
        /// or IAudioInputDevice
        /// </summary>
        private List<IAudioInputDependency> mDependencies = new List<IAudioInputDependency>();





        void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("Attempted to create two AudioInputExample. This should never happen. Deactivating script.");
                this.enabled = false;
                return;
            }
            instance = this;
        }

        void Start()
        {
            //we first initialize the global / shared factory to ensure everything works
            UnityCallFactory.EnsureInit(OnCallFactoryReady, OnCallFactoryFailed);
        }

        /// <summary>
        /// Once the global factory is ready we either use it or
        /// create our own.
        /// </summary>
        protected virtual void OnCallFactoryReady()
        {
            mIsReady = true;
            Debug.LogWarning("Accessing native plugin internals. This example does not work with WebGL or UWP!");
            if (USE_UNIQUE_FACTORY)
            {
                var factory = new WebRtcCSharp.PeerFactoryConfig();
                mFactory = new NativeAwrtcFactory(factory);
                //This should not interfere with the global factory's access to the Microphone
                mFactory.NativeFactory.GetAudioManager().SetAllowRecording(false);
            }
            else
            {
                mFactory = UnityCallFactory.Instance.InternalFactory as NativeAwrtcFactory;
                //This will break any default CallApp that run in the same application
                Debug.LogWarning("Deactivating plugin side recording globally. Only virtual audio input is supported after this.");
                mFactory.NativeFactory.GetAudioManager().SetAllowRecording(false);
            }
            mInput = mFactory.NativeFactory.GetAudioInput();

            InitDeps();
            foreach (var v in sInitCallbacks) {
                v.Item1();
            }
            sInitCallbacks.Clear();
            Debug.Log("AudioInputExample ready");
        }
        /// <summary>
        /// Initialized scripts that depend on the factory
        /// </summary>
        private void InitDeps()
        {
            var deps = this.GetComponentsInChildren<IAudioInputDependency>();
            foreach (var d in deps)
            {
                AddInternal(d);
            }
        }

        private void AddInternal(IAudioInputDependency d)
        {
            d.SetupDevice(mInput);
            mDependencies.Add(d);
        }

        /// <summary>
        /// For use by UnityMicrophoneManager to add new audio devices on the fly
        /// </summary>
        /// <param name="dev"></param>
        public void AddDevice(IAudioInputDevice dev)
        {
            AddInternal(dev);
        }

        /// <summary>
        /// Used by UnityMicrophoneManager to destroy audio devices
        /// </summary>
        /// <param name="dev"></param>
        public void RemDevice(IAudioInputDevice dev)
        {
            dev.DestroyDevice();
            mDependencies.Remove(dev);
        }

        protected virtual void OnCallFactoryFailed(string error)
        {
            Debug.LogError(error);
            foreach (var v in sInitCallbacks)
            {
                v.Item2(error);
            }
        }


        /// <summary>
        /// Should be called by any dependencies that get destroyed
        /// </summary>
        /// <param name="dep"></param>
        public void OnDependencyDestroyed(IAudioInputDevice dep)
        {
            mDependencies.Remove(dep);
        }

        void OnDestroy()
        {
            if (USE_UNIQUE_FACTORY && mFactory != null)
            {
                Debug.Log("Clearing dependency list");
                foreach (var v in mDependencies.ToArray())
                {
                    v.DestroyDevice();
                    mDependencies.Remove(v);
                }
                Debug.LogWarning("Disposing the custom factory.");
                mFactory.Dispose();
                mFactory = null;
            }
        }



        /// <summary>
        /// Called by an App before starting to use a virtual input device. 
        /// </summary>
        /// <param name="name"></param>
        public void StartAccessDevice(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            foreach(var v in mDependencies)
            {
                var device = v as IAudioInputDevice;
                if (device?.DeviceName == name)
                    device.OnAccessStart();
            }
        }
        /// <summary>
        /// Called by AudioInput after stopping to use a virtual device. 
        /// This allows the device to shut down and conserve resources.
        /// </summary>
        /// <param name="name"></param>
        public void StopAccessDevice(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            foreach (var v in mDependencies)
            {
                var device = v as IAudioInputDevice;
                if (device?.DeviceName == name)
                    device.OnAccessStop();
            }
        }


        public string[] GetAudioInputDevices()
        {
            List<string> devices = new List<string>();
            foreach (var v in mDependencies)
            {
                var device = v as IAudioInputDevice;
                if (device != null)
                    devices.Add(device.DeviceName);

            }
            return devices.ToArray();
        }

        public ICall CreateCall(NetworkConfig config)
        {
            return mFactory.CreateCall(config);
        }


        /// <summary>
        /// Buffers all user script callbacks that try to ensure the init process is completed
        /// via EnsureInit. Will be emptied after init completed or failed
        /// </summary>
        private static List<(Action, Action<string>)> sInitCallbacks = new List<(Action, Action<string>)>();
        public static void EnsureInit(Action onSuccessCallback, Action<string> onFailureCallback)
        {
            //set to warning for regular use
            UnityCallFactory.RequestLogLevelStatic(UnityCallFactory.LogLevel.Info);

            if (instance != null && instance.mIsReady)
            {
                onSuccessCallback();
                return;
            }
            else
            {
                var val = (onSuccessCallback, onFailureCallback );
                sInitCallbacks.Add(val);
            }

        }
    }
}