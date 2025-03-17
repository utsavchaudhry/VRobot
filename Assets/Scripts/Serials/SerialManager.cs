using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class SerialManager : MonoBehaviour
{
    public static SerialManager Instance { get; private set; }

    private MicroSerial tc;
    private MicroSerial m;
    private MicroSerial xiaomi;

    private List<MicroSerial> serials;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(Instance);
        }
        Instance = this;
    }

    private void Start()
    {
        serials = FindObjectsOfType<MicroSerial>().ToList();
    }

    private void Update()
    {
        if (!(tc && m && xiaomi))
        {
            foreach (MicroSerial serial in serials)
            {
                switch (serial.Micro)
                {
                    case MICRO.NOT_FOUND:
                        break;
                    case MICRO.TC_MOTORS:
                        tc = serial;
                        break;
                    case MICRO.M_MOTORS:
                        m = serial;
                        break;
                    case MICRO.XIAOMI_MOTORS:
                        xiaomi = serial;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public bool SendSerialMessage(MICRO micro, string msg)
    {
        MicroSerial serial = micro switch
        {
            MICRO.NOT_FOUND => null,
            MICRO.TC_MOTORS => tc,
            MICRO.M_MOTORS => m,
            MICRO.XIAOMI_MOTORS => xiaomi,
            _ => null,
        };

        return serial && serial.SendSerialMessage(msg);
    }
}
