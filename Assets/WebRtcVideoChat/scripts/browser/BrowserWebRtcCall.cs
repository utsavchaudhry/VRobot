/* 
 * Copyright (C) 2023 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */

namespace Byn.Awrtc.Browser
{
    public class BrowserWebRtcCall : AWebRtcCall
    {
        private NetworkConfig mConfig;

        public BrowserWebRtcCall(NetworkConfig config) :
            base(config)
        {
            mConfig = config;
            Initialize(CreateNetwork());
        }

        private IMediaNetwork CreateNetwork()
        {
            return new BrowserMediaNetwork(mConfig);
        }
        public void RequestStats()
        {
            if (this.mNetwork != null)
            {
                (mNetwork as BrowserMediaNetwork).RequestStats();
            }
        }

        public void SetVolumePan(float volume, float pan, ConnectionId remoteUserId)
        {
            if (this.mNetwork != null)
            {
                (mNetwork as BrowserMediaNetwork).SetVolumePan(volume, pan, remoteUserId);
            }
        }
        public override void Update()
        {
            base.Update();
            if (this.mNetwork != null)
            {
                var net = (mNetwork as BrowserMediaNetwork);
                RtcEvent evt;
                while ((evt = net.DequeueRtcEvent()) != null)
                {

                    CallEventArgs args = new RtcEventArgs(evt);
                    TriggerCallEvent(args);
                }
            }

        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {

                //Dispose was called by the user -> cleanup other managed objects

                //cleanup the internal network
                if (mNetwork != null)
                    mNetwork.Dispose();
                mNetwork = null;

                //unregister on network factory to allow garbage collection
                //if (this.mFactory != null)
                //    this.mFactory.OnCallDisposed(this);
                //this.mFactory = null;
            }
        }
    }
}
