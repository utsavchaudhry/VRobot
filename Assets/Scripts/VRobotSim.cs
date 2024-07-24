using UnityEngine;
using System.Linq;

public class VRobotSim : MonoBehaviour
{
    public static VRobotSim Instance { get; private set; }

    [System.Serializable]
    private class ServoSim
    {
        [SerializeField] private Transform target;
        [System.Serializable] private enum Axis { X, Y, Z }
        [SerializeField] private Axis axis;
        [SerializeField] private int offset;
        [SerializeField] private bool flip;

        private Vector3 defaultAngles;
        private bool initialized;

        public void Set(int angle)
        {
            if (!target)
            {
                return;
            }

            if (!initialized)
            {
                defaultAngles = target.localEulerAngles;
                initialized = true;
            }

            Vector3 desiredAngles = defaultAngles;

            int min = -90 + offset;
            int max = 90 + offset;

            angle = Mathf.RoundToInt(min + (angle / 180f * (max - min)));

            if (flip)
            {
                angle = min + (max - angle);
            }

            switch (axis)
            {
                case Axis.X:
                    desiredAngles.x = angle;
                    break;
                case Axis.Y:
                    desiredAngles.y = angle;
                    break;
                case Axis.Z:
                    desiredAngles.z = angle;
                    break;
                default:
                    break;
            }

            target.localEulerAngles = desiredAngles;
        }
    }

    [SerializeField] private ServoSim[] joints;
    [SerializeField] private Transform avatar;
    [SerializeField] private Vector3 headPositionOffset;

    private Transform head;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        head = Camera.main.transform;
    }

    public void Set(string servoMessage)
    {
        int[] angles = servoMessage.Split(',').Select(int.Parse).ToArray();
        int maxIndex = Mathf.Min(angles.Length, joints.Length);
        for (int i = 0; i < maxIndex; i++)
        {
            joints[i].Set(angles[i]);
        }
    }

    private void Update()
    {
        if (head)
        {
            transform.position = head.transform.position + headPositionOffset;
        }

        if (avatar)
        {
            transform.forward = avatar.forward;
        }
    }
}
