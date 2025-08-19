/* Copyright (c) 2019-present Evereal. All rights reserved. */

namespace Evereal.VideoCapture
{
  public interface IVideoCapture : ICapture
  {
    // When video encoding complete
    void OnEncoderComplete(string path);

    // When audio muxing complete
    void OnMuxerComplete(string path);

    // Get ffmpeg encoder instance
    FFmpegEncoder GetFFmpegEncoder();

    // Get GPU encoder instance
    GPUEncoder GetGPUEncoder();
  }
}