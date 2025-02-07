using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class XRJoystickDifferentialDrive : MonoBehaviour
{
    [Header("Differential Drive Settings")]
    [Tooltip("Max linear speed for forward/backward.")]
    public float maxLinearSpeed = 1.0f;

    [Tooltip("Max turn speed (higher = faster turns).")]
    public float maxTurnSpeed = 1.0f;

    // Exposed wheel speeds so other scripts can read them
    public float LeftWheelSpeed { get; private set; }
    public float RightWheelSpeed { get; private set; }

    // Internal reference to the left-hand XR device
    private InputDevice leftHandDevice;

    void Start()
    {
        // Get the left-hand controller device
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, inputDevices);

        if (inputDevices.Count > 0)
        {
            leftHandDevice = inputDevices[0];
            Debug.Log("Found left hand controller: " + leftHandDevice.name);
        }
        else
        {
            Debug.LogWarning("No left hand controller found. Make sure your Quest is active.");
        }
    }

    void Update()
    {
        // If the device is not valid, don’t attempt to get input
        if (!leftHandDevice.isValid) return;

        // Attempt to read the primary 2D axis (the thumbstick on Quest)
        if (leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
        {
            // thumbstick.y = forward/back, thumbstick.x = left/right
            float forwardSpeed = thumbstick.y * maxLinearSpeed;
            float turnSpeed = thumbstick.x * maxTurnSpeed;

            // Differential drive logic:
            //   Left  wheel = forwardSpeed + turnSpeed
            //   Right wheel = forwardSpeed - turnSpeed
            LeftWheelSpeed = forwardSpeed + turnSpeed;
            RightWheelSpeed = forwardSpeed - turnSpeed;
        }
        else
        {
            // If we fail to read the axis, set speeds to zero (optional)
            LeftWheelSpeed = 0f;
            RightWheelSpeed = 0f;
        }
    }
}
