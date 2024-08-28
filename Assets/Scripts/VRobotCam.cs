using UnityEngine;

public class VRobotCam : MonoBehaviour
{
    [SerializeField] private Material leftEyeMaterial;
    [SerializeField] private Material rightEyeMaterial;

    [Space]

    [SerializeField] private int width = 1280;
    [SerializeField] private int height = 720;
    [SerializeField] private int fps = 30;

    private WebCamTexture webcamTexture;

    void Start()
    {
        webcamTexture = new WebCamTexture(width * 2, height, fps);

        webcamTexture.Play();

        leftEyeMaterial.SetTexture("_MainTex", webcamTexture);
        rightEyeMaterial.SetTexture("_MainTex", webcamTexture);

        Debug.Log("Width: " + webcamTexture.width + "\n" +
            "Height: " + webcamTexture.height);
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
