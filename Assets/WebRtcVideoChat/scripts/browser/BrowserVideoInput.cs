using System;
using Byn.Awrtc;

namespace Byn.Awrtc.Browser
{
    public class BrowserVideoInput : IVideoInput
    {
        public VideoInputFormat InputFormat {
            get
            {
                return VideoInputFormat.ABGR;
            }
        }

        public void AddDevice(string name, int width, int height, int fps)
        {
            CAPI.Unity_VideoInput_AddDevice(name, width, height, fps);
        }

        public void RemoveDevice(string name)
        {
            CAPI.Unity_VideoInput_RemoveDevice(name);
        }
        public bool UpdateFrame(string name, byte[] dataBuffer, int width, int height, int rotation, bool firstRowIsBottom)
        {
            return CAPI.Unity_VideoInput_UpdateFrame(name, dataBuffer, 0, dataBuffer.Length, width, height, rotation, firstRowIsBottom);
        }
        public bool UpdateFrame(string name, IntPtr dataPtr, int length, int width, int height, int rotation, bool firstRowIsBottom)
        {
            return CAPI.Unity_VideoInput_UpdateFrame(name, dataPtr, 0, length, width, height, rotation, firstRowIsBottom);
        }
    }
}


