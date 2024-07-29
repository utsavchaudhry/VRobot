using UnityEngine;

public class VRobotCam : MonoBehaviour
{
    private WebCamTexture webCamTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            // Automatically select the first available camera
            webCamTexture = new WebCamTexture(devices[0].name, 2560, 720);
            webCamTexture.Play();
        }
        else
        {
            Debug.LogError("No camera found!");
        }
    }

    void OnGUI()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            GUI.DrawTexture(new Rect(0, 0, webCamTexture.width, webCamTexture.height), webCamTexture);
        }
    }
}
