using Byn.Unity.Examples;
using Evereal.VideoCapture;
using UnityEngine;

public class MyVideoRecorder : MonoBehaviour
{
    private enum CaptureState { Idle, Recoding, Saving }

    [System.Serializable]
    private struct MyRecorder
    {
        [SerializeField] private RenderTexture texture;
        [SerializeField] private VideoCapture capture;
        [SerializeField] private bool flipX;
        [SerializeField] private bool flipY;
        [SerializeField] private bool enable;

        private Vector2 scale;
        private Vector2 offset;
        private CaptureState state;
        private bool init;

        private void Init()
        {
            scale = new Vector2(1f, -1f);
            offset = new Vector2(0f, 1f);

            if (flipX)
            {
                scale.x *= -1f;
            }

            if (flipY)
            {
                scale.y *= -1f;
            }

            offset.x = scale.x == 1f ? 0f : 1f;
            offset.y = scale.y == 1f ? 0f : 1f;

            state = CaptureState.Idle;
            capture.OnComplete += Saved;

            init = true;
        }

        private void Saved(object sender, CaptureCompleteEventArgs e)
        {
            state = CaptureState.Idle;
        }

        public void Des()
        {
            if (capture && init)
            {
                capture.OnComplete -= Saved;
            }
        }

        public void Set(Texture2D texture2D)
        {
            if (!enable)
            {
                return;
            }

            if (!init)
            {
                Init();
            }

            Graphics.Blit(texture2D, texture, scale, offset);

            if (state == CaptureState.Idle && capture.StartCapture())
            {
                state = CaptureState.Recoding;
            }
        }

        public void Stop()
        {
            if (state == CaptureState.Recoding && capture.StopCapture())
            {
                state = CaptureState.Saving;
            }
        }
    }

    [SerializeField] private MyRecorder client;
    [SerializeField] private MyRecorder host;

    private ConferenceApp conference;

    private void Start()
    {
        conference = FindObjectOfType<ConferenceApp>();
    }

    private void OnDestroy()
    {
        client.Des();
        host.Des();
    }

    private void LateUpdate()
    {
        if (conference.clientVideoTexture)
        {
            client.Set(conference.clientVideoTexture);
        }
        else
        {
            client.Stop();
        }

        if (conference.HostVideoTexture && conference.clientVideoTexture)
        {
            host.Set(conference.HostVideoTexture);
        }
        else
        {
            host.Stop();
        }
    }
}
