using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ServoMapper : MonoBehaviour
{
    public static ServoMapper Instance { get; private set; }

    public bool IsReady { get; private set; }

    [Serializable]
    private class ServoMotor
    {
        [Range(0, 360)] public int range = 180;

        [SerializeField] private int minPWM = 100;
        [SerializeField] private int maxPWM = 600;
        [SerializeField] [Range(-180f, 180f)] private float startAngle = -90f;
        [SerializeField] [Range(-180f, 180f)] private float offset;
        [SerializeField] private bool flip;
        [SerializeField] private bool log;

        public int CalculatePWM(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            int pwm = Mathf.RoundToInt(minPWM + ((maxPWM - minPWM) * NormalizeValue(angle + offset, startAngle, startAngle + range)));

            if (flip)
            {
                pwm = maxPWM + minPWM - pwm;
            }

            if (log)
            {
                Debug.Log(pwm);
            }

            return pwm;
        }

        public float ClampAngle(float angle)
        {
            return Mathf.Clamp(angle + offset, startAngle, startAngle + range);
        }

        private float NormalizeValue(float value, float min, float max)
        {
            // Ensure that max is greater than min to avoid division by zero.
            if (max <= min)
            {
                throw new ArgumentException("max must be greater than min");
            }

            // Clamp the value between min and max to avoid going beyond the bounds.
            float clampedValue = Mathf.Clamp(value, min, max);

            // Normalize the clamped value to a range of 0 to 1.
            float normalizedValue = (clampedValue - min) / (max - min);

            return normalizedValue;
        }

    }

    [Serializable]
    private class ServoJoint : ServoMotor
    {
        private enum Axis { X, Y, Z }

        [SerializeField] private Transform target;
        [SerializeField] private Axis axis;
        [SerializeField] private bool useUnityEulerConversion = false;

        public int GetPWM()
        {
            return CalculatePWM(target ? (axis switch
            {
                Axis.X => useUnityEulerConversion ? target.localEulerAngles.x : QuaternionToEulerAngles(target.localRotation).x,
                Axis.Y => useUnityEulerConversion ? target.localEulerAngles.y : QuaternionToEulerAngles(target.localRotation).y,
                Axis.Z => useUnityEulerConversion ? target.localEulerAngles.z : QuaternionToEulerAngles(target.localRotation).z,
                _ => useUnityEulerConversion ? target.localEulerAngles.x : QuaternionToEulerAngles(target.localRotation).x,
            }) : 0f);
        }

        private Vector3 QuaternionToEulerAngles(Quaternion q)
        {
            // Roll (x-axis rotation)
            float sinr_cosp = 2 * (q.w * q.x + q.y * q.z);
            float cosr_cosp = 1 - 2 * (q.x * q.x + q.y * q.y);
            float roll = Mathf.Atan2(sinr_cosp, cosr_cosp);

            // Pitch (y-axis rotation)
            float sinp = 2 * (q.w * q.y - q.z * q.x);
            float pitch;
            if (Mathf.Abs(sinp) >= 1)
                pitch = Mathf.Sign(sinp) * Mathf.PI / 2; // Use 90 degrees if out of range
            else
                pitch = Mathf.Asin(sinp);

            // Yaw (z-axis rotation)
            float siny_cosp = 2 * (q.w * q.z + q.x * q.y);
            float cosy_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
            float yaw = Mathf.Atan2(siny_cosp, cosy_cosp);

            // Convert radians to degrees
            return new Vector3(roll * Mathf.Rad2Deg, pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg);
        }
    }

    [SerializeField] private ServoMotor rGrapper;
    [SerializeField] private ServoMotor rWrist;
    [SerializeField] private ServoJoint[] rArm;

    [Space]

    [SerializeField] private ServoMotor lGrapper;
    [SerializeField] private ServoMotor lWrist;
    [SerializeField] private ServoJoint[] lArm;

    [Space]

    [SerializeField] private float wristTurnSpeed;

    [Space]

    [SerializeField] private Transform head;
    [SerializeField] private ServoMotor yaw;
    [SerializeField] private ServoMotor pitch;

    private InputDevice leftController;
    private InputDevice rightController;
    private float yawOffset;
    private float rWristAngle;
    private float lWristAngle;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(Instance);
        }

        Instance = this;
    }

    private void Start()
    {
        InputManager.OnPrimaryButtonDown += ResetYaw;

        Invoke(nameof(ResetYaw), 1f);
    }

    private void SetDevices()
    {
        if (rightController.isValid && leftController.isValid)
        {
            return;
        }

        List<InputDevice> devices = new();

        if (!rightController.isValid)
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0)
            {
                rightController = devices[0];
            }
        }

        if (!leftController.isValid)
        {
            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0)
            {
                leftController = devices[0];
            }
        }
    }

    private void OnDestroy()
    {
        InputManager.OnPrimaryButtonDown -= ResetYaw;
    }

    public string GetServoMessage()
    {
        _ = rightController.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        string servoMessage = rGrapper.CalculatePWM(triggerValue * rGrapper.range).ToString();

        _ = rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue);
        rWristAngle = rWrist.ClampAngle(rWristAngle + (joystickValue.x * Time.deltaTime * wristTurnSpeed));
        servoMessage += "," + rWrist.CalculatePWM(rWristAngle);

        foreach (ServoJoint joint in rArm)
        {
            servoMessage += "," + joint.GetPWM();
        }

        _ = leftController.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
        servoMessage += "," + lGrapper.CalculatePWM(triggerValue * lGrapper.range);

        _ = leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickValue);
        lWristAngle = lWrist.ClampAngle(lWristAngle + (joystickValue.x * Time.deltaTime * wristTurnSpeed));
        servoMessage += "," + lWrist.CalculatePWM(lWristAngle);

        foreach (ServoJoint joint in lArm)
        {
            servoMessage += "," + joint.GetPWM();
        }

        if (head)
        {
            servoMessage += "," + yaw.CalculatePWM(Mathf.Repeat(head.localEulerAngles.y - yawOffset, 360f)) + "," + pitch.CalculatePWM(head.localEulerAngles.x);
        }

        return servoMessage;
    }

    private void ResetYaw()
    {
        yawOffset = head.localEulerAngles.y;

        if (yawOffset > 180f)
        {
            yawOffset -= 360f;
        }

        VRobot.ResetYaw(head.eulerAngles.y);

        Debug.Log("Yaw Offset: " + yawOffset);

        IsReady = true;
    }

    private void Update()
    {
        SetDevices();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetYaw();
        }
    }
}