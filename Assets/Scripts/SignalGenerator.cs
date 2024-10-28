using UnityEngine;

public class SignalGenerator : MonoBehaviour
{
    public static string Signal { get; private set; }
    public static bool IsReady { get; private set; }

    [SerializeField] private Transform head;

    [SerializeField] private ServoJoint[] joints;
    private float yawOffset;

    private void Awake()
    {
        IsReady = false;
    }

    private void Start()
    {
        InputManager.LeftController.PrimaryBtn.OnDown += ResetYaw;
        Invoke(nameof(ResetYaw), 1f);
    }

    private void OnDestroy()
    {
        InputManager.LeftController.PrimaryBtn.OnDown -= ResetYaw;
    }

    private void Update()
    {
        string msg = string.Empty;

        foreach (ServoJoint joint in joints)
        {
            msg += joint.Signal + ",";
        }

        Signal = msg.Trim(',');
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
}
