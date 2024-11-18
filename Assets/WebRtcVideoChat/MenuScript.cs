/* 
 * Copyright (C) 2022 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using Byn.Awrtc.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Byn.Unity.Examples
{
    public class MenuScript : MonoBehaviour
    {

        private static bool sCreated = false;
        public RectTransform _StartMenu;
        public Button _ButtonMenu;

        private void Awake()
        {
            if (sCreated)
            {
                Destroy(this.gameObject);
            }
            sCreated = true;
        }

        void Start()
        {
            Debug.Log("Setting fps to 60");
            Application.targetFrameRate = 60;
            DontDestroyOnLoad(this.gameObject);
            MenuUi();
        }

        private void PrintDeviceDebug()
        {
#if !UNITY_WEBGL && !UNITY_WSA
            var testFactory = new WebRtcCSharp.RTCPeerConnectionFactoryRef();
            Debug.Log(testFactory.GetDebugDeviceInfo());
            testFactory.Dispose();
#endif
        }


        // Update is called once per frame
        void Update()
        {
        }

        public void Initialize()
        {
            //just to trigger the init process
            var res = UnityCallFactory.Instance;


        }

        public void ExampleUi()
        {

            _ButtonMenu.gameObject.SetActive(true);
            _StartMenu.gameObject.SetActive(false);

        }

        public void MenuUi()
        {
            _ButtonMenu.gameObject.SetActive(false);
            _StartMenu.gameObject.SetActive(true);
        }

        private void LoadExample(string s)
        {
            ExampleUi();
            UnityEngine.SceneManagement.SceneManager.LoadScene(s);
        }

        public void LoadChatExample()
        {
            LoadExample("chatscene");
        }

        public void LoadCallExample()
        {
            LoadExample("callscene");
        }

        public void LoadVideoInput()
        {
            LoadExample("VideoInput");
        }
        public void LoadAudioInput()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                //these two will be merged in the future to provide a cross-platform API
                Debug.LogWarning("AudioInput is not supported in WebGL. Using AudioWeb instead.");
                LoadExample("AudioWeb");
            }
            else
            {
                LoadExample("AudioInput");
            }
        }
        public void LoadConference()
        {
            LoadExample("conferencescene");
        }

        public void LoadMenu()
        {
            MenuUi();
            UnityEngine.SceneManagement.SceneManager.LoadScene("menuscene");
        }
    }
}