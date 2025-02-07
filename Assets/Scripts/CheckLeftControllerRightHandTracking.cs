using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CheckLeftControllerRightHandTracking : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the OVRHand component for the right hand.")]
    public OVRHand rightHand;

    [Tooltip("The transform of the left controller (e.g., from OVRControllerPrefab).")]
    public Transform leftControllerTransform;

    [Tooltip("TextMeshProUGUI component for displaying logs.")]
    public TextMeshProUGUI logText;

    private string logBuffer = ""; // Buffer to store log text

    void Update()
    {
        // Clear log buffer for the frame
        logBuffer = "";

        // 1. Check if the left controller (LTouch) is connected:
        bool isLeftControllerActive = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);

        // 2. Check if the right hand is tracked using hand tracking:
        bool isRightHandTracked = rightHand && rightHand.IsTracked;

        // 3. Ensure both are actively tracked/connected
        if (isLeftControllerActive && isRightHandTracked)
        {
            // --- LEFT CONTROLLER POSE ---
            Vector3 leftControllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            Quaternion leftControllerRot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);

            logBuffer += $"[Left Controller] Position: {leftControllerPos}\n";
            logBuffer += $"[Left Controller] Rotation: {leftControllerRot.eulerAngles}\n";

            if (leftControllerTransform != null)
            {
                logBuffer += $"[Left Controller Transform] Position: {leftControllerTransform.position}\n";
                logBuffer += $"[Left Controller Transform] Rotation: {leftControllerTransform.rotation.eulerAngles}\n";
            }

            // --- RIGHT HAND POSE ---
            Vector3 rightHandPosition = rightHand.transform.position;
            Quaternion rightHandRotation = rightHand.transform.rotation;

            logBuffer += $"[Right Hand] Position: {rightHandPosition}\n";
            logBuffer += $"[Right Hand] Rotation: {rightHandRotation.eulerAngles}\n";

            // --- RAW FINGER TRACKING DATA ---
            for (int i = 0; i < 5; i++)
            {
                var finger = (OVRHand.HandFinger)i;
                float pinchStrength = rightHand.GetFingerPinchStrength(finger);
                logBuffer += $"[Right Hand] Finger: {finger}, Pinch Strength: {pinchStrength}\n";
            }

            // Optional: Retrieve bone data
            OVRSkeleton handSkeleton = rightHand.GetComponent<OVRSkeleton>();
            if (handSkeleton != null)
            {
                List<OVRBone> fingerBones = new(handSkeleton.Bones);
                for (int b = 0; b < fingerBones.Count; b++)
                {
                    OVRBone bone = fingerBones[b];
                    logBuffer += $"[Right Hand Bone {b}] Name: {bone.Id}, Local Pos: {bone.Transform.localPosition}, Local Rot: {bone.Transform.localRotation}\n";
                }
            }
        }
        else
        {
            // Warnings if something is not tracked properly
            if (!isLeftControllerActive)
            {
                logBuffer += "Left Controller is not connected or not active!\n";
            }
            if (!isRightHandTracked)
            {
                logBuffer += "Right Hand is not tracked or OVRHand reference is missing!\n";
            }
        }

        // Update the TextMeshProUGUI component
        if (logText != null)
        {
            logText.text = logBuffer;
        }
        else
        {
            Debug.LogWarning("LogText (TextMeshProUGUI) component is not assigned!");
        }
    }
}
