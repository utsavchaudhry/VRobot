using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ServoMapper : MonoBehaviour
{
    public static ServoMapper Instance { get; private set; }

    [System.Serializable]
    private abstract class ServoMotor
    {
        [SerializeField] [Range(0, 360)] protected int range = 180;
        [SerializeField] private int minPWM = 100;
        [SerializeField] private int maxPWM = 600;
        [SerializeField] [Range(-180f, 180f)] private float startAngle = -90f;
        [SerializeField] private bool flip;

        public int CalculatePWM(float angle)
        {
            Debug.Log(angle);

            if (angle > 180f)
            {
                angle -= 360f;
            }

            angle = Mathf.Clamp(angle, startAngle, startAngle + range);

            if (startAngle < 0)
            {
                angle += Mathf.Abs(startAngle);
            }

            int servoAngle = Mathf.RoundToInt(minPWM + ((maxPWM - minPWM) * angle / range));

            return flip ? range - servoAngle : servoAngle;
        }
    }

    [System.Serializable]
    private class ServoJoint : ServoMotor
    {
        private enum Axis { X, Y, Z }

        [SerializeField] private Transform target;
        [SerializeField] private Axis axis;

        public int GetPWM()
        {
            return CalculatePWM(axis switch
            {
                Axis.X => target.localRotation.eulerAngles.x,
                Axis.Y => target.localRotation.eulerAngles.y,
                Axis.Z => target.localRotation.eulerAngles.z,
                _ => target.localRotation.eulerAngles.x,
            });
        }
    }

    [System.Serializable]
    private class ServoGrapper : ServoMotor
    {
        private InputDevice inputDevice;

        public bool IsXRControllerValid()
        {
            return inputDevice.isValid;
        }

        public void Initialize(bool isLeft)
        {
            List<InputDevice> devices = new();

            if (isLeft)
            {
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
            }
            else
            {
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
            }

            if (devices.Count > 0)
            {
                inputDevice = devices[0];
            }
        }

        public int GetPWM()
        {
            _ = inputDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
            return CalculatePWM(triggerValue * range);
        }
    }

    [SerializeField] private ServoGrapper rGrapper;
    [SerializeField] private ServoJoint[] rArm;
    [SerializeField] private ServoGrapper lGrapper;
    [SerializeField] private ServoJoint[] lArm;
    [SerializeField] private ServoJoint[] head;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(Instance);
        }

        Instance = this;
    }

    public string GetServoMessage()
    {
        if (!rGrapper.IsXRControllerValid())
        {
            rGrapper.Initialize(false);
        }

        string servoMessage = rGrapper.GetPWM().ToString();

        foreach (ServoJoint joint in rArm)
        {
            servoMessage += "," + joint.GetPWM();
        }

        if (!lGrapper.IsXRControllerValid())
        {
            lGrapper.Initialize(true);
        }

        servoMessage += "," + lGrapper.GetPWM();

        foreach (ServoJoint joint in lArm)
        {
            servoMessage += "," + joint.GetPWM();
        }

        foreach (ServoJoint joint in head)
        {
            servoMessage += "," + joint.GetPWM();
        }

        return servoMessage;
    }

    private void Update()
    {
        _ = rArm[2].GetPWM();

        //Debug.Log(GetServoMessage());
    }
}