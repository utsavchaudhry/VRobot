/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using System.Collections;
using Byn.Awrtc.Unity;
using UnityEngine;
namespace Byn.Unity.Examples
{
    /// <summary>
    /// This class works only for WebGL. There are no alternative features to this in other platforms.
    /// 
    /// If WebGL is used this class will add several additional virtual cameras which allow streaming from Unity's Canvas html element that
    /// is used to render the Unity scene. These devices are a lot faster than the regular VideoInput devices because they don't need
    /// any copy from C# to java script. Instead they use the browsers ability to stream directly from a HTMLCanvasElement via the
    /// captureStream method.
    /// https://developer.mozilla.org/en-US/docs/Web/API/HTMLCanvasElement/captureStream
    /// 
    /// Note that the actual streaming FPS and the FPS shown in the CallApp UI might differ. The WebRTC API for browsers
    /// does not have a standardized way of returning the actual framerate.  Only webkit based browsers return accurate
    /// numbers. Others might return FPS based on the track meta data (might be inaccurate) or they have to default to 
    /// 30 FPS (or whatever is set in the typescript implementation BrowserMediaStream.DEFAULT_FRAMERATE). 
    /// 
    /// Make sure your scene / rendered image doesn't contain any transparent areas otherwise the previous frames might shine through the transparent regions!
    /// e.g. if you create a new Unity Camera with a solid color Unity sets the background to transparent automatically. Make sure to set Alpha to 255!
    /// </summary>
    public class WebGlCanvasDevice : MonoBehaviour
    {

#if UNITY_WEBGL && !UNITY_EDITOR
    //HTML query. "canvas" for the first canvas html element
    //Use "#myid" to query the webpage for a specific element with the id "myid"
    //will use captureStream()
    public readonly static string canvasSourceQuery = "canvas";

    public readonly static string canvasDevice1Name = "canvas default";
    //will use captureStream(25)
    public readonly static string canvasDevice2Name = "canvas 25 FPS";

    public readonly static int canvasDevice3Fps = 10;
    //scaling not yet supported. will just ignore the resolution for now.
    public readonly static string canvasDevice3Name = "canvas 640x360 " + canvasDevice3Fps + " FPS";
    //Set to false if not needed to save come CPU time
    private readonly static bool activateDevice3 = false;

    /// <summary>
    /// Only important for canvasDevice3 because it needs to scale the image before sending.
    /// 
    /// If set to false:
    ///     The frame will be accessed & scaled during unity's Update loop. This only works if Unity has the 
    ///     flag "preserveDrawingBuffer" set to true. See: https://docs.unity3d.com/Manual/webgl-graphics.html
    ///     Otherwise the image won't show up because WebGL clears the image after showing it via the screen
    ///     
    /// If set to true:
    ///     We wait until the end of the frame and capture the image before Unity clears the Canvas again. In this case
    ///     we don't need the "preserveDrawingBuffer" flag. Unity won't have cleared the background image though. 
    ///     
    /// </summary>
    public readonly static bool captureEndOfFrame = true;
    

    private void Awake()
    {


        UnityCallFactory.EnsureInit(() =>
        {
                Debug.Log("Adding unity canvas as videoinput device");
                Byn.Awrtc.Browser.CAPI.Unity_VideoInput_AddCanvasDevice(canvasSourceQuery, canvasDevice1Name, 0, 0, 0);
                Byn.Awrtc.Browser.CAPI.Unity_VideoInput_AddCanvasDevice(canvasSourceQuery, canvasDevice2Name, 0, 0, 25);
                if(activateDevice3){
                    //if width & height are not 0 it will turn on scaling. This requires additional processing each frame
                    //e.g. for 10 FPS the method Unity_VideoInput_UpdateFrame needs to be called 10 times / sec
                    Byn.Awrtc.Browser.CAPI.Unity_VideoInput_AddCanvasDevice(canvasSourceQuery, canvasDevice3Name, 640, 360, canvasDevice3Fps);
                }                
                //only needed if a device that needs scaling is used
                if(activateDevice3){
                    StartCoroutine(UpdateVideo());
                }
        });
    }

    private void OnDestroy()
    {
        Debug.Log("Adding unity canvas as videoinput device");
        Byn.Awrtc.Browser.CAPI.Unity_VideoInput_RemoveDevice(canvasDevice1Name);
        Byn.Awrtc.Browser.CAPI.Unity_VideoInput_RemoveDevice(canvasDevice2Name);
        if(activateDevice3){
            Byn.Awrtc.Browser.CAPI.Unity_VideoInput_RemoveDevice(canvasDevice3Name);
        }
    }


    private IEnumerator UpdateVideo()
    {
        //Best to run this only if this device is actually needed!
        //as this can waste a lot of performance
        while (true)
        {
            yield return new WaitForSecondsRealtime(1.0f / canvasDevice3Fps);
            if(captureEndOfFrame)
                yield return new WaitForEndOfFrame();
            Byn.Awrtc.Browser.CAPI.Unity_VideoInput_UpdateFrame(canvasDevice3Name, null, 0, 0, 0, 0, 0, false);
        }
    }

#endif
    }
}