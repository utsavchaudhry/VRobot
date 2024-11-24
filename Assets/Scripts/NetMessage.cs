using UnityEngine;
using System.Collections;
using Byn.Unity.Examples;
using System.Linq;

public class NetMessage : MonoBehaviour
{
    [SerializeField] private bool log;

    private void Start()
    {
        _ = StartCoroutine(GenerateMessage());
    }

    private IEnumerator GenerateMessage()
    {
        ServoJoint[] servoJoints = FindObjectsOfType<ServoJoint>().OrderBy(j => j.GetMotorID()).ToArray();
        Clamp clamp = FindObjectOfType<Clamp>();
        ChatApp _chatAppobj = FindObjectOfType<ChatApp>();

        while (_chatAppobj)
        {
            if (!VRobot.IsPaused)
            {
                string _msg = string.Empty;
                int currentID = 1;

                foreach (ServoJoint joint in servoJoints)
                {
                    if (clamp.GetMotorID() == currentID)
                    {
                        _msg += clamp.GetCurrentSignal() + ",";
                    }
                    else
                    {
                        while (currentID < joint.GetMotorID())
                        {
                            _msg += ",";
                            currentID++;
                        }
                        _msg += joint.GetCurrentSignal() + ",";
                    }
                    currentID++;
                }

                _msg = _msg.Trim(',');

                _chatAppobj.SendButtonPressed(_msg);

                if (log)
                {
                    Debug.Log(_msg);
                }
            }

            yield return null;
        }
    }
}
