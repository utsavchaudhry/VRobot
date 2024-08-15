using UnityEngine;

public class VRobotCam : MonoBehaviour
{
    public Material leftEyeMaterial;
    public Material rightEyeMaterial;

    private WebCamTexture webcamTexture;
    private int width = 1280;   // should match your camera
    private int height = 720;

    void Start()
    {
        webcamTexture = new WebCamTexture(width * 2, height);

        webcamTexture.Play();

        leftEyeMaterial.SetTexture("_MainTex", webcamTexture);
        rightEyeMaterial.SetTexture("_MainTex", webcamTexture);
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
