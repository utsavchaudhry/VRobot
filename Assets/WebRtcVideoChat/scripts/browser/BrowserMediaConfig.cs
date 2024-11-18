using Byn.Awrtc;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Byn.Awrtc.Browser
{
    public class BrowserMediaConfig : MediaConfig
    {
        private string mAudioInputDevice = "";
        public string AudioInputDevice
        {
            get
            {
                return mAudioInputDevice;
            }
            set
            {
                mAudioInputDevice = value;
            }
        }
        /// <summary>
        /// Creates a new object using default settings 
        /// </summary>
        public BrowserMediaConfig() : base()
        {

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="config"></param>
        protected BrowserMediaConfig(BrowserMediaConfig config) : base(config)
        {
            this.mAudioInputDevice = config.mAudioInputDevice;
        }

        /// <summary>
        /// Clones all members
        /// </summary>
        /// <returns>Cloned object of type NativeMediaConfig</returns>
        public override MediaConfig DeepClone()
        {
            return new BrowserMediaConfig(this);
        }

        public override bool Equals(object obj)
        {
            BrowserMediaConfig other = obj as BrowserMediaConfig;
            if (other == null)
                return false;

            if (base.Equals(obj) == false)
                return false;


            return this.mAudioInputDevice == other.mAudioInputDevice;
        }
        /// <summary>
        /// Avoid. Not optimized.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        protected override void ToStringBase(StringBuilder sb)
        {
            base.ToStringBase(sb);
            sb.Append(", AudioInput:" + this.AudioInputDevice);
        }
    }
}