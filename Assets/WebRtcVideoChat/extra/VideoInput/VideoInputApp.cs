/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using Byn.Awrtc.Unity;
using System.Collections;
using UnityEngine;

namespace Byn.Unity.Examples
{

    /// <summary>
    /// This app is the same as the callapp just refreshing the
    /// video list later to give the virtual video input some time to
    /// setup.
    /// 
    /// See VirtualCamera for documentation.
    /// </summary>
    public class VideoInputApp : CallApp
    {

        protected override void Start()
        {
            base.Start();
            //need to fresh the ui a bit later as the virtual input needs a while to start
            //without this the virtual camera wouldn't be visible in the video device list
            StartCoroutine(CoroutineRefreshLater());
        }
        protected override void OnCallFactoryReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogWarning("Enabling webgl screen share option");
            Byn.Awrtc.Browser.CAPI.Unity_Media_EnableScreenCapture("Screen capture", true);
#endif
            base.OnCallFactoryReady();
        }

        IEnumerator CoroutineRefreshLater()
        {
            yield return new WaitForSecondsRealtime(1);
            mUi.UpdateVideoDropdown();
        }
    }
}