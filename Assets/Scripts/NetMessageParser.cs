using UnityEngine;
using System.Collections.Generic;
using Byn.Unity.Examples;
using TMPro;

public class NetMessageParser : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] [TextArea] private string defaultPosition;
    [SerializeField] private bool log;

    private Dictionary<int, int> signals;
    private bool paused;

    private void Start()
    {
        signals = new Dictionary<int, int>();

        ChatApp.OnMsgReceived += Parse;
    }

    private void OnDestroy()
    {
        ChatApp.OnMsgReceived -= Parse;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            paused = !paused;
            if (statusText)
            {
                statusText.text = paused ? "Paused" : string.Empty;
            }
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            Parse(defaultPosition);
        }
    }

    private void Parse(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg) || !msg.Contains(',') || paused)
        {
            return;
        }

        msg = msg[(msg.IndexOf(':') + 1)..];

        int id = 1;

        string[] signalStream = msg.Split(',');
        for (int i = 0; i < signalStream.Length; i++)
        {
            string part = signalStream[i];
            if (!string.IsNullOrWhiteSpace(part))
            {
                if (int.TryParse(part, out int signal))
                {
                    bool send = !signals.ContainsKey(id);

                    if (!send)
                    {
                        send = Mathf.Abs(signal - signals[id]) >= 15f;
                    }

                    if (send)
                    {
                        string command = id + "," + signal;

                        if (SerialHandler.SendSerialData(command))
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
                }
                else
                {
                    return;
                }
            }

            id++;
        }

        //_ = SerialHandlerWheels.SendSerialData(signalStream[^2] + "," + signalStream[^1]);
    }
}
