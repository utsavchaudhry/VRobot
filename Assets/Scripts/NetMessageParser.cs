using UnityEngine;
using System.Collections.Generic;
using Byn.Unity.Examples;

public class NetMessageParser : MonoBehaviour
{
    [SerializeField] private bool log;

    private Dictionary<int, int> signals;

    private void Start()
    {
        signals = new Dictionary<int, int>();

        ChatApp.OnMsgReceived += Parse;
    }

    private void OnDestroy()
    {
        ChatApp.OnMsgReceived -= Parse;
    }

    private void Parse(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg) || !msg.Contains(','))
        {
            return;
        }

        msg = msg[(msg.IndexOf(':') + 1)..];

        int id = 1;

        foreach (string part in msg.Split(','))
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                if (int.TryParse(part, out int signal))
                {
                    bool send = !signals.ContainsKey(id);

                    if (!send)
                    {
                        send = Mathf.Abs(signal - signals[id]) >= 15f;
                    }

                    string command = id + "," + signal;

                    if (send && SerialHandler.SendSerialData(command))
                    {
                        if (log)
                        {
                            Debug.Log(command);
                        }

                        if (!signals.TryAdd(id, signal))
                        {
                            signals[id] = signal;
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            id++;
        }
    }
}
