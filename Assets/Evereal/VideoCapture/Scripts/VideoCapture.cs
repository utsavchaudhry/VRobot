/* Copyright (c) 2019-present Evereal. All rights reserved. */

using System;
//using System.Threading;
using UnityEngine;

namespace Evereal.VideoCapture
{
  /// <summary>
  /// <c>VideoCapture</c> component, manage and record gameplay video from specific camera.
  /// Work with ffmpeg encoder or GPU encoder component to generate gameplay videos.
  /// </summary>
  [Serializable]
  public class VideoCapture : CaptureBase, IVideoCapture
  {
    #region Properties

    [Tooltip("Encoding GPU acceleration will improve performance significantly, but only available for Windows with dedicated graphic card and H.264 codec.")]
    [SerializeField]
    public bool gpuEncoding = false;
    // FFmpeg Encoder
    public FFmpegEncoder ffmpegEncoder;
    // GPU Encoder
    public GPUEncoder gpuEncoder;

    /// <summary>
    /// Private properties.
    /// </summary>
    // The garbage collection thread.
    //private Thread garbageCollectionThread;
    //private static bool garbageThreadRunning = false;

    #endregion

    #region Video Capture

    /// <summary>
    /// Initialize the attributes of the capture session and start capture.
    /// </summary>
    public override bool StartCapture()
    {
      if (!PrepareCapture())
      {
        return false;
      }

      if (offlineRender)
      {
        Time.captureFramerate = frameRate;
      }

      // init ffmpeg encoding settings
      FFmpegEncoderSettings();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

      if (gpuEncoding)
      {
        if (FreeTrial.Check())
        {
          Debug.LogFormat(LOG_FORMAT, "GPU encoding is not supported in free trial version, fall back to software encoding.");
          gpuEncoding = false;
        }

        // init GPU encoding settings
        GPUEncoderSettings();

        if (!gpuEncoder.instantiated || !gpuEncoder.IsSupported())
        {
          Debug.LogFormat(LOG_FORMAT, "GPU encoding is not supported in current device or settings, fall back to software encoding.");
          gpuEncoding = false;
        }
      }

#else

      if (gpuEncoding)
      {
        Debug.LogFormat(LOG_FORMAT, "GPU encoding is only available on windows system, fall back to software encoding.");
        gpuEncoding = false;
      }

#endif

      // Init audio recorder
      if (!gpuEncoding && captureAudio)
      {
        if (captureMicrophone)
        {
          if (MicrophoneRecorder.singleton == null)
          {
            gameObject.AddComponent<MicrophoneRecorder>();
          }
          MicrophoneRecorder.singleton.saveFolderFullPath = saveFolderFullPath;
          MicrophoneRecorder.singleton.captureType = captureType;
          MicrophoneRecorder.singleton.deviceIndex = deviceIndex;
          audioRecorder = MicrophoneRecorder.singleton;
        }
        else
        {
          if (AudioRecorder.singleton == null)
          {
            if (GetComponent<DontDestroy>() != null)
            {
              // Reset AudioListener
              AudioListener listener = FindObjectOfType<AudioListener>();
              if (listener)
              {
                Destroy(listener);
                Debug.LogFormat(LOG_FORMAT, "AudioListener found, reset in game scene.");
              }
              gameObject.AddComponent<AudioListener>();
              gameObject.AddComponent<AudioRecorder>();
            }
            else
            {
              // Keep AudioListener
              AudioListener listener = FindObjectOfType<AudioListener>();
              if (!listener)
              {
                listener = gameObject.AddComponent<AudioListener>();
                Debug.LogFormat(LOG_FORMAT, "AudioListener not found, add a new AudioListener.");
              }
              listener.gameObject.AddComponent<AudioRecorder>();
            }
          }
          AudioRecorder.singleton.saveFolderFullPath = saveFolderFullPath;
          AudioRecorder.singleton.captureType = captureType;
          audioRecorder = AudioRecorder.singleton;
        }
      }

      // Init ffmpeg muxer
      if (!gpuEncoding && captureAudio)
      {
        if (FFmpegMuxer.singleton == null)
        {
          gameObject.AddComponent<FFmpegMuxer>();
        }
        FFmpegMuxer.singleton.ffmpegFullPath = ffmpegFullPath;
        FFmpegMuxer.singleton.saveFolderFullPath = saveFolderFullPath;
        FFmpegMuxer.singleton.AttachVideoCapture(this);
      }

      // Init ffmpeg streamer
      if (!gpuEncoding && captureType == CaptureType.LIVE)
      {
        if (FFmpegStreamer.singleton == null)
        {
          gameObject.AddComponent<FFmpegStreamer>();
        }
        FFmpegStreamer.singleton.ffmpegFullPath = ffmpegFullPath;
        FFmpegStreamer.singleton.captureAudio = captureAudio;
        FFmpegStreamer.singleton.liveStreamUrl = liveStreamUrl;
        FFmpegStreamer.singleton.bitrate = bitrate;
      }

      if (gpuEncoding)
      {
        if (!gpuEncoder.StartCapture())
        {
          OnCaptureError(new CaptureErrorEventArgs(CaptureErrorCode.VIDEO_CAPTURE_START_FAILED));
          return false;
        }
      }
      else
      {
        if (!ffmpegEncoder.StartCapture())
        {
          OnCaptureError(new CaptureErrorEventArgs(CaptureErrorCode.VIDEO_CAPTURE_START_FAILED));
          return false;
        }

        if (captureAudio && !audioRecorder.RecordStarted())
        {
          audioRecorder.StartRecord();
        }

        if (captureType == CaptureType.LIVE)
        {
          // start ffmpeg live streamer
          if (!FFmpegStreamer.singleton.streamStarted)
          {
            FFmpegStreamer.singleton.StartStream();
          }
        }
      }

      // Create a blitter object to keep frames presented on the screen
      CreateBlitterInstance();

      // Update current status.
      status = CaptureStatus.STARTED;

      // Start garbage collect thread.
      //if (!garbageThreadRunning)
      //{
      //  garbageThreadRunning = true;

      //  if (garbageCollectionThread != null &&
      //    garbageCollectionThread.IsAlive)
      //  {
      //    garbageCollectionThread.Abort();
      //    garbageCollectionThread = null;
      //  }

      //  garbageCollectionThread = new Thread(GarbageCollectionProcess);
      //  garbageCollectionThread.Priority = System.Threading.ThreadPriority.Lowest;
      //  garbageCollectionThread.IsBackground = true;
      //  garbageCollectionThread.Start();
      //}

      Debug.LogFormat(LOG_FORMAT, "Video capture session started.");
      return true;
    }

    /// <summary>
    /// Stop capturing and produce the finalized video. Note that the video file may not be completely written when this method returns. In order to know when the video file is complete, register <c>OnComplete</c> delegate.
    /// </summary>
    public override bool StopCapture()
    {
      if (status != CaptureStatus.STARTED)
      {
        Debug.LogWarningFormat(LOG_FORMAT, "Video capture session not start yet!");
        return false;
      }

      if (offlineRender)
      {
        // Restore captureFramerate states.
        Time.captureFramerate = 0;
      }

      // pending for video encoding process
      status = CaptureStatus.STOPPED;

      if (gpuEncoding && gpuEncoder.captureStarted)
      {
        gpuEncoder.StopCapture();
      }

      if (!gpuEncoding && ffmpegEncoder.captureStarted)
      {
        ffmpegEncoder.StopCapture();

        if (captureAudio && audioRecorder.RecordStarted())
        {
          audioRecorder.StopRecord();
        }

        if (captureType == CaptureType.VOD)
        {
          if (captureAudio)
          {
            FFmpegMuxer.singleton.SetAudioFile(audioRecorder.GetRecordedAudio());

            if (!FFmpegMuxer.singleton.muxInitiated)
            {
              FFmpegMuxer.singleton.InitMux();
            }
          }

          Debug.LogFormat(LOG_FORMAT, "Video capture session stopped, generating video...");
        }
        else if (captureType == CaptureType.LIVE && FFmpegStreamer.singleton.streamStarted)
        {
          FFmpegStreamer.singleton.StopStream();
        }
      }

      // Reset camera settings.
      ResetCameraSettings();

      // Restore screen render.
      ClearBlitterInstance();

      return true;
    }

    /// <summary>
    /// Cancel capturing and clean temp files.
    /// </summary>
    public override bool CancelCapture()
    {
      if (status != CaptureStatus.STARTED)
      {
        Debug.LogWarningFormat(LOG_FORMAT, "Video capture session not start yet!");
        return false;
      }

      if (offlineRender)
      {
        // Restore captureFramerate states.
        Time.captureFramerate = 0;
      }

      if (gpuEncoding && gpuEncoder.captureStarted)
      {
        gpuEncoder.CancelCapture();
      }

      if (!gpuEncoding && ffmpegEncoder.captureStarted)
      {
        ffmpegEncoder.CancelCapture();

        if (captureAudio && audioRecorder.RecordStarted())
        {
          audioRecorder.CancelRecord();
        }

        if (captureType == CaptureType.LIVE && FFmpegStreamer.singleton.streamStarted)
        {
          FFmpegStreamer.singleton.StopStream();
        }
      }

      Debug.LogFormat(LOG_FORMAT, "Video capture session canceled.");

      // reset video capture status
      status = CaptureStatus.READY;

      // Reset camera settings.
      ResetCameraSettings();

      // Restore screen render.
      ClearBlitterInstance();

      return true;
    }

    public FFmpegEncoder GetFFmpegEncoder()
    {
      return ffmpegEncoder;
    }

    public GPUEncoder GetGPUEncoder()
    {
      return gpuEncoder;
    }

    /// <summary>
    /// Handle callbacks for the video encoder complete.
    /// </summary>
    /// <param name="savePath">Video save path.</param>
    public void OnEncoderComplete(string savePath)
    {
      if (captureType == CaptureType.LIVE)
      {
        status = CaptureStatus.READY;

        EnqueueCompleteEvent(new CaptureCompleteEventArgs(liveStreamUrl));

        Debug.LogFormat(LOG_FORMAT, "Live streaming session success!");
      }
      else if (captureType == CaptureType.VOD)
      {
        if (gpuEncoding || !captureAudio) // No audio capture required, done!
        {
          status = CaptureStatus.READY;

          EnqueueCompleteEvent(new CaptureCompleteEventArgs(savePath));

          lastVideoFile = savePath;

          Debug.LogFormat(LOG_FORMAT, "Video capture session success!");
        }
        else
        {
          // Enqueue video file
          FFmpegMuxer.singleton.EnqueueVideoFile(savePath);
          // Pending for ffmpeg audio capture and muxing
          status = CaptureStatus.PENDING;
        }
      }
    }

    /// <summary>
    /// Handle audio process complete when capture audio.
    /// </summary>
    /// <param name="savePath">Final muxing video path.</param>
    public void OnMuxerComplete(string savePath)
    {
      status = CaptureStatus.READY;

      EnqueueCompleteEvent(new CaptureCompleteEventArgs(savePath));

      lastVideoFile = savePath;

      Debug.LogFormat(LOG_FORMAT, "Video generated success!");
    }

    #endregion

    #region Internal

    private void GPUEncoderSettings()
    {
      gpuEncoder.regularCamera = regularCamera;
      gpuEncoder.stereoCamera = stereoCamera;
      gpuEncoder.captureSource = captureSource;
      gpuEncoder.captureType = captureType;
      gpuEncoder.captureMode = captureMode;
      gpuEncoder.resolutionPreset = resolutionPreset;
      gpuEncoder.frameWidth = frameWidth;
      gpuEncoder.frameHeight = frameHeight;
      gpuEncoder.cubemapSize = cubemapSize;
      gpuEncoder.bitrate = bitrate;
      gpuEncoder.frameRate = frameRate;
      gpuEncoder.projectionType = projectionType;
      gpuEncoder.liveStreamUrl = liveStreamUrl;
      gpuEncoder.stereoMode = stereoMode;
      gpuEncoder.interpupillaryDistance = interpupillaryDistance;
      gpuEncoder.captureAudio = captureAudio;
      gpuEncoder.captureMicrophone = captureMicrophone;
      if (deviceIndex < gpuEncoder.GetMicDevicesCount())
        gpuEncoder.SetMicDevice((uint)deviceIndex);
      gpuEncoder.antiAliasing = antiAliasingSetting;
      gpuEncoder.inputTexture = inputTexture;
      gpuEncoder.offlineRender = offlineRender;
      gpuEncoder.saveFolderFullPath = saveFolderFullPath;
    }

    private void FFmpegEncoderSettings()
    {
      ffmpegEncoder.regularCamera = regularCamera;
      ffmpegEncoder.stereoCamera = stereoCamera;
      ffmpegEncoder.captureSource = captureSource;
      ffmpegEncoder.captureType = captureType;
      ffmpegEncoder.captureMode = captureMode;
      ffmpegEncoder.encoderPreset = encoderPreset;
      ffmpegEncoder.resolutionPreset = resolutionPreset;
      ffmpegEncoder.frameWidth = frameWidth;
      ffmpegEncoder.frameHeight = frameHeight;
      ffmpegEncoder.cubemapSize = cubemapSize;
      ffmpegEncoder.bitrate = bitrate;
      ffmpegEncoder.frameRate = frameRate;
      ffmpegEncoder.projectionType = projectionType;
      ffmpegEncoder.liveStreamUrl = liveStreamUrl;
      ffmpegEncoder.stereoMode = stereoMode;
      ffmpegEncoder.interpupillaryDistance = interpupillaryDistance;
      ffmpegEncoder.captureAudio = captureAudio;
      //ffmpegEncoder.captureMicrophone = captureMicrophone;
      ffmpegEncoder.antiAliasing = antiAliasingSetting;
      ffmpegEncoder.inputTexture = inputTexture;
      ffmpegEncoder.offlineRender = offlineRender;
      ffmpegEncoder.ffmpegFullPath = ffmpegFullPath;
      ffmpegEncoder.saveFolderFullPath = saveFolderFullPath;
    }

    //void GarbageCollectionProcess()
    //{
    //  double deltaTime = 1 / (double)frameRate;
    //  int sleepTime = (int)(deltaTime * 1000);
    //  while (status != CaptureStatus.READY)
    //  {
    //    Thread.Sleep(sleepTime);
    //    System.GC.Collect();
    //  }

    //  garbageThreadRunning = false;
    //}

    #endregion

    #region Unity Lifecycle

    protected new void Awake()
    {
      base.Awake();

      if (ffmpegEncoder == null)
      {
        ffmpegEncoder = GetComponentInChildren<FFmpegEncoder>(true);
        if (ffmpegEncoder == null)
        {
          Debug.LogErrorFormat(LOG_FORMAT,
           "Component FFmpegEncoder not found, please use prefab or follow the document to set up video capture.");
          return;
        }
      }

      if (gpuEncoder == null)
      {
        gpuEncoder = GetComponentInChildren<GPUEncoder>(true);
        if (gpuEncoder == null)
        {
          Debug.LogErrorFormat(LOG_FORMAT,
           "Component hardware encoder not found, please use prefab or follow the document to set up video capture.");
        }
      }
    }

    private void OnEnable()
    {
      if (ffmpegEncoder != null)
        ffmpegEncoder.OnComplete += OnEncoderComplete;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
      if (gpuEncoder != null)
      {
        gpuEncoder.gameObject.SetActive(true);
        gpuEncoder.OnComplete += OnEncoderComplete;
      }
#endif
    }

    private void OnDisable()
    {
      if (ffmpegEncoder != null)
        ffmpegEncoder.OnComplete -= OnEncoderComplete;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
      if (gpuEncoder != null)
        gpuEncoder.OnComplete -= OnEncoderComplete;
#endif
    }

    #endregion

  }
}