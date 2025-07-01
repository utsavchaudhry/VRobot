using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
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

        if (SerialManager.Instance)
        {
            _ = SerialManager.Instance.SendSerialMessage(MICRO.TC_MOTORS, "c:" + (id + 20).ToString() + "," + Signal.ToString());
        }
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
    [SerializeField] private InputDeviceCharacteristics xrDevice;

    private List<FingerMotor> motorListSorted;

    private InputDevice targetDevice;

    // Flag to indicate whether a valid device has been found.
    private bool deviceInitialized = false;

    // Public properties to expose the sensor values.
    public float TriggerValue { get; private set; }
    public float GripValue { get; private set; }

    [SerializeField] private bool log;
    [SerializeField] private TextMeshProUGUI logText;

    [SerializeField] private TextMeshProUGUI btnText;
    [SerializeField] private Image btnImage;

    private enum Mode { VR, PC, Mobile }
    [SerializeField] private Mode mode;

    private Color openColor;
    private Color closedColor;

    private void Start()
    {
        if (btnImage)
        {
            openColor = btnImage.color;
        }
        closedColor = new Color(1f, 0f, 0f, Mathf.Clamp01(openColor.a * 3f));

        motorListSorted = index.motors.Concat(middle.motors)
            .Concat(ring.motors)
            .Concat(pinky.motors)
            .Concat(thumb.motors)
            .OrderBy(m => m.GetID())
            .ToList();

        UpdateBtnText();
    }

    private void UpdateBtnText()
    {
        if (btnText)
        {
            btnText.text = (TriggerValue == 0f ? "Close\n" : "Open\n") + (xrDevice == InputDeviceCharacteristics.Left ? "Left " : "Right ") + "Hand";
        }

        if (btnImage)
        {
            _ = btnImage.DOKill();
            _ = btnImage.DOColor(TriggerValue == 0f ? openColor : closedColor, 0.15f);
        }
    }

    private void InitializeDevice()
    {
        List<InputDevice> devices = new();
        InputDevices.GetDevicesWithCharacteristics(xrDevice, devices);

        if (devices.Count > 0)
        {
            targetDevice = devices[0];
            deviceInitialized = true;
        }
    }

    public void Toggle()
    {
        TriggerValue = TriggerValue == 0f ? 1f : 0f;
        UpdateBtnText();
    }

    private void Update()
    {
        switch (mode)
        {
            case Mode.VR:
                if (!deviceInitialized)
                {
                    InitializeDevice();
                }
                if (deviceInitialized)
                {
                    TriggerValue = targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerVal) ? triggerVal : 0f;
                    GripValue = targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue) ? gripValue : 0f;
                }
                break;
            case Mode.PC:
                TriggerValue = Input.GetMouseButton(xrDevice == InputDeviceCharacteristics.Left ? 0 : 1) ? 1f : 0f;
                break;
            case Mode.Mobile:
                break;
            default:
                break;
        }

        if (mode != Mode.VR)
        {
            GripValue = TriggerValue;
        }

        index.CalculateSignal(TriggerValue);
        middle.CalculateSignal(GripValue);
        ring.CalculateSignal(GripValue);
        pinky.CalculateSignal(GripValue);
        thumb.CalculateSignal(TriggerValue);

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
