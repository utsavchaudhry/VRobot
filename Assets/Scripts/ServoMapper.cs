using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ServoMapper : MonoBehaviour
{
    public enum Side { L, R }
    public enum BodyJoint { ShoulderForward, ShoulderLateral, Elbow, Wrist, Finger }
    public enum HeadAxis { Pitch, Yaw }

    [System.Serializable]
    private class Arm
    {
        [SerializeField] private Transform[] joints;

        private InputDevice inputDevice;

        public void Initialize(Side side)
        {
            List<InputDevice> devices = new();

            if (side == Side.L)
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

        public int GetAngle(BodyJoint joint)
        {
            float angle = 90f;

            switch (joint)
            {
                case BodyJoint.ShoulderForward:
                    angle = joints[0].localEulerAngles.y;
                    if (angle > 180)
                    {
                        angle -= 360f;
                    }
                    angle += 90f;
                    angle = 180f - angle;
                    break;
                case BodyJoint.ShoulderLateral:
                    angle = joints[1].localEulerAngles.z;
                    if (angle > 180f)
                    {
                        angle -= 360f;
                    }
                    angle -= 52f;
                    break;
                case BodyJoint.Elbow:
                    angle = 180f - Vector3.Angle(joints[3].position - joints[2].position, joints[1].position - joints[2].position);
                    break;
                case BodyJoint.Wrist:
                    angle = joints[3].localEulerAngles.z + 5f;
                    break;
                case BodyJoint.Finger:
                    if (inputDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
                    {
                        return Mathf.RoundToInt(triggerValue * 180f);
                    }
                    break;
                default:
                    break;
            }

            return Mathf.RoundToInt(Mathf.Clamp(Mathf.Abs(angle), 0f, 180f));
        }
    }

    public static ServoMapper Instance { get; private set; }

    [SerializeField] private Arm leftArm;
    [SerializeField] private Arm rightArm;

    private Transform head;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        leftArm.Initialize(Side.L);
        rightArm.Initialize(Side.R);

        head = Camera.main.transform;
    }

    public int GetAngle(Side side, BodyJoint joint)
    {
        return side == Side.L ? leftArm.GetAngle(joint) : rightArm.GetAngle(joint);
    }

    public int GetHeadAngle(HeadAxis axis)
    {
        if (!head)
        {
            return 90;
        }

        float angle = 90f;

        switch (axis)
        {
            case HeadAxis.Pitch:
                angle = head.eulerAngles.x;
                break;
            case HeadAxis.Yaw:
                angle = head.eulerAngles.y;
                break;
            default:
                break;
        }

        if (angle > 180f)
        {
            angle -= 360f;
        }

        angle += 90f;

        if (axis == HeadAxis.Yaw)
        {
            angle = 180f - angle;
        }

        return Mathf.RoundToInt(angle);
    }
}