using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ServoMapper : MonoBehaviour
{
    public enum Side { L, R }
    public enum BodyJoint { ShoulderForward, ShoulderLateral, Elbow, Palm, Wrist, Finger }
    public enum HeadAxis { Pitch, Yaw }

    [System.Serializable]
    private class Arm
    {
        [SerializeField] private Transform shoulder;
        [SerializeField] private Transform elbow;
        [SerializeField] private Transform wrist;
        [SerializeField] private Transform finger;

        private Transform avatar;
        private InputDevice inputDevice;

        public void Initialize(Transform _avatar, Side side)
        {
            avatar = _avatar;

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
            switch (joint)
            {
                case BodyJoint.ShoulderForward:
                    if (shoulder && avatar)
                    {
                        Vector3 shoulderDirection = Vector3.ProjectOnPlane(-shoulder.right, avatar.right);
                        return Mathf.RoundToInt(180f - Vector3.Angle(shoulderDirection, avatar.forward));
                    }
                    break;
                case BodyJoint.ShoulderLateral:
                    if (shoulder && avatar)
                    {
                        Vector3 shoulderDirection = Vector3.ProjectOnPlane(-shoulder.right, avatar.forward);
                        return Mathf.RoundToInt(Vector3.Angle(shoulderDirection, avatar.up));
                    }
                    break;
                case BodyJoint.Elbow:
                    if (shoulder && elbow && wrist)
                    {
                        return Mathf.RoundToInt(Vector3.Angle(elbow.position - shoulder.position, wrist.position - elbow.position));
                    }
                    break;
                case BodyJoint.Palm:
                    if (elbow && wrist && finger && avatar)
                    {
                        int angle = Mathf.RoundToInt(Vector3.Angle(wrist.position - elbow.position, finger.position - wrist.position));
                        Vector3 palmDirection = Vector3.ProjectOnPlane(-wrist.right, avatar.forward);
                        return 90 + Mathf.RoundToInt(Vector3.Angle(palmDirection, avatar.right) < 90f ? angle : -angle);
                    }
                    break;
                case BodyJoint.Wrist:
                    if (wrist && avatar)
                    {
                        Vector3 wristDirection = Vector3.ProjectOnPlane(wrist.up, avatar.forward);
                        return Mathf.RoundToInt(180f - Vector3.Angle(wristDirection, avatar.up));
                    }
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

            return 90;
        }
    }

    public static ServoMapper Instance { get; private set; }

    [Header("Transform References")]

    [SerializeField] private Transform avatar;
    [SerializeField] private Arm leftArm;
    [SerializeField] private Arm rightArm;

    private Transform head;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        leftArm.Initialize(avatar, Side.L);
        rightArm.Initialize(avatar, Side.R);

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

        return Mathf.RoundToInt(angle + 90f);
    }
}