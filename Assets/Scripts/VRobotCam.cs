using UnityEngine;

public class VRobotCam : MonoBehaviour
{
    private WebCamTexture webCamTexture;

    public Renderer leftEyeQuad;  // Assign these in the inspector
    public Renderer rightEyeQuad;

    private int desiredWidth = 2560;
    private int desiredHeight = 720;

    private Texture2D leftEyeTexture;
    private Texture2D rightEyeTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            // Automatically select the first available camera
            webCamTexture = new WebCamTexture(devices[0].name, desiredWidth, desiredHeight);
            webCamTexture.Play();

            // Initialize the Texture2D for left and right eye
            leftEyeTexture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            rightEyeTexture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);

            // Assign the textures to the quads
            leftEyeQuad.material.mainTexture = leftEyeTexture;
            rightEyeQuad.material.mainTexture = rightEyeTexture;
        }
        else
        {
            Debug.LogError("No camera found!");
        }
    }

    void Update()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            // Get the full pixel data from the webcam texture
            Color32[] fullPixels = webCamTexture.GetPixels32();

            // Extract left eye image (1280x720 portion from the left side)
            Color32[] leftPixels = new Color32[1280 * 720];
            for (int y = 0; y < 720; y++)
            {
                for (int x = 0; x < 1280; x++)
                {
                    leftPixels[y * 1280 + x] = fullPixels[y * desiredWidth + x];
                }
            }
            leftEyeTexture.SetPixels32(leftPixels);
            leftEyeTexture.Apply();

            // Extract right eye image (1280x720 portion from the right side)
            Color32[] rightPixels = new Color32[1280 * 720];
            for (int y = 0; y < 720; y++)
            {
                for (int x = 0; x < 1280; x++)
                {
                    rightPixels[y * 1280 + x] = fullPixels[y * desiredWidth + (x + 1280)];
                }
            }
            rightEyeTexture.SetPixels32(rightPixels);
            rightEyeTexture.Apply();
        }
    }
}
