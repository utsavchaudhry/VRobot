using UnityEngine;
using System.Collections;

public class SignalGenerator : MonoBehaviour
{
    public static string Signal { get; private set; }

    [SerializeField] private ServoJoint[] joints;

    private string signal;

    private void Start()
    {
        _ = StartCoroutine(GenerateCombinedSignal());
    }

    private IEnumerator GenerateCombinedSignal()
    {
        string msg;

        while (true)
        {
            msg = string.Empty;

            foreach (ServoJoint joint in joints)
            {
                msg += joint.Signal + ",";
            }

            Signal = msg.Trim(',');

            signal = Signal;

            yield return null;
        }
    }
}
