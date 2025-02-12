using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.Linq;
using TMPro;

[System.Serializable]
public class FingerMotor
{
    public int Signal { get; private set; }

    [SerializeField] private int id;
    [SerializeField] private int minPWM;
    [SerializeField] private int maxPWM;
    [SerializeField] private bool flip;

    public int GetID()
    {
        return id;
    }

    public void CalculateSignal(float input)
    {
        input = Mathf.Clamp01(input);

        if (flip)
        {
            input = 1f - input;
        }

        Signal = Mathf.RoundToInt(minPWM + ((maxPWM - minPWM) * input));

        _ = SerialHandlerWheels.SendSerialData(id + "," + Signal);
    }
}

[System.Serializable]
public class Finger
{
    public FingerMotor[] motors;

    public void CalculateSignal(float input)
    {
        foreach (FingerMotor motor in motors)
        {
            motor.CalculateSignal(input);
        }
    }
}

public class JoystickFingerSignalGenerator : MonoBehaviour
{
    public static string Signal { get; private set; }

    [SerializeField] private Finger index;
    [SerializeField] private Finger middle;
    [SerializeField] private Finger ring;
    [SerializeField] private Finger pinky;
    [SerializeField] private Finger thumb;

    private List<FingerMotor> motorListSorted;

    private void Start()
    {
        motorListSorted = index.motors.Concat(middle.motors)
            .Concat(ring.motors)
            .Concat(pinky.motors)
            .Concat(thumb.motors)
            .OrderBy(m => m.GetID())
            .ToList();
    }

    private InputDevice targetDevice;

    // Flag to indicate whether a valid device has been found.
    private bool deviceInitialized = false;

    // Public properties to expose the sensor values.
    public float TriggerValue { get; private set; }
    public float GripValue { get; private set; }
    public float ThumbRestValue { get; private set; }

    /// <summary>
    /// Attempt to initialize the XR input device.
    /// Here, we search for a device with the characteristics of a held-in-hand controller.
    /// </summary>
    private void InitializeDevice()
    {
        var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand |
                                     InputDeviceCharacteristics.Controller |
                                     InputDeviceCharacteristics.Left;
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, devices);

        if (devices.Count > 0)
        {
            targetDevice = devices[0];
            deviceInitialized = true;
            Debug.Log($"Controller found: {targetDevice.name}");
        }
        else
        {
            Debug.LogWarning("Meta Quest 3 controller not found. Please ensure the controller is active.");
        }
    }

    [SerializeField] private bool log;
    [SerializeField] private TextMeshProUGUI logText;

    /// <summary>
    /// In each frame, the script queries the XR device for the current sensor values.
    /// </summary>
    private void Update()
    {
        // If the device has not been initialized yet, attempt to locate it.
        if (!deviceInitialized)
        {
            InitializeDevice();
        }

        if (deviceInitialized)
        {
            // Retrieve the analog trigger value (0 to 1).
            if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerVal))
            {
                TriggerValue = triggerVal;
            }
            else
            {
                TriggerValue = 0f;
            }

            // Retrieve the analog grip value (0 to 1).
            if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripVal))
            {
                GripValue = gripVal;
            }
            else
            {
                GripValue = 0f;
            }

            ThumbRestValue = targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryBtn) && primaryBtn ? 1f : 0f;
        }

        index.CalculateSignal(TriggerValue);
        middle.CalculateSignal(GripValue);
        ring.CalculateSignal(GripValue);
        pinky.CalculateSignal(GripValue);
        thumb.CalculateSignal(ThumbRestValue);

        Signal = string.Join(",", motorListSorted.Select(m => m.Signal));

        if (log)
        {
            Debug.Log(Signal);
        }

        if (logText)
        {
            logText.text = Signal.ToString();
        }
    }
}
