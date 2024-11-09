using UnityEngine;

public class SignalGenerator : MonoBehaviour
{
    public static string Signal { get; private set; }

    [SerializeField] private ServoJoint[] joints;

    private void Update()
    {
        string msg = string.Empty;

        foreach (ServoJoint joint in joints)
        {
            msg += joint.Signal + ",";
        }

        Signal = msg.Trim(',');

        //SerialCommunicator.WriteToSerial(Signal);
    }
}
