using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class VRobotCam : MonoBehaviour
{
    // Import the functions from the CameraAccess.dll
    [DllImport("CameraAccess.dll")]
    private static extern int StartCapture();

    [DllImport("CameraAccess.dll")]
    private static extern int StopCapture();

    [DllImport("CameraAccess.dll")]
    private static extern IntPtr GetFrame();

    public Material leftEyeMaterial;  // Assign these in the inspector
    public Material rightEyeMaterial;

    private Texture2D texture;
    private int width = 1280;   // Example resolution, should match your camera
    private int height = 720;   // Example resolution, should match your camera
    private IntPtr framePtr;

    void Start()
    {
        // Start capturing from the camera
        StartCapture();

        // Initialize the texture that will hold the camera feed
        texture = new Texture2D(width * 2, height, TextureFormat.RGBA32, false); // Assuming stereo video feed

        // Assign the texture to the materials
        leftEyeMaterial.SetTexture("_MainTex", texture);
        rightEyeMaterial.SetTexture("_MainTex", texture);
    }

    void Update()
    {
        // Get the current frame from the camera
        framePtr = GetFrame();
        if (framePtr != IntPtr.Zero)
        {
            // Load the raw data into the texture
            texture.LoadRawTextureData(framePtr, width * height * 4 * 2); // 2 frames (left & right), 4 bytes per pixel
            texture.Apply();

            // The shader will automatically split the stereo texture for the left and right eye
        }
    }

    void OnDestroy()
    {
        // Stop capturing when the application is closed
        StopCapture();
    }
}
