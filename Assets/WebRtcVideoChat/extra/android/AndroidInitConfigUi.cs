/* 
 * Copyright (C) 2022 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using UnityEngine;
using UnityEngine.UI;

namespace Byn.Awrtc.Unity
{
    /// <summary>
    /// UI for setting flags to test android hw acceleration.
    /// Can only be used before the first use of UnityCallFactory.
    /// </summary>
    public class AndroidInitConfigUi : MonoBehaviour
    {
        public Toggle hardwareAcc;
        public Toggle useTextures;
        public Toggle forcePref;
        public Dropdown codec;

        void Start()
        {

        }


        void Update()
        {

        }

        public void Init()
        {
            AndroidInitConfig config = new AndroidInitConfig();
            config.hardwareAcceleration = hardwareAcc.isOn;
            config.useTextures = useTextures.isOn;
            if (codec.value != 0)
            {
                config.preferredCodec = codec.options[codec.value].text;
            }
            config.forcePreferredCodec = forcePref.isOn;

            Debug.Log("Setting android init config: " + config);
            UnityCallFactory.AndroidConfig = config;
            UnityCallFactory.EnsureInit(() =>
            {
                Debug.Log("Init complete. ");

                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("menuscene");
            });
        }
    }
}
