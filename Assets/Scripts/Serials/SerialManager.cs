using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class SerialManager : MonoBehaviour
{
    private MicroSerial sc;
    private MicroSerial st;
    private MicroSerial xiaomi;

    private List<MicroSerial> serials;
    private bool setupComplete;

    private void Start()
    {
        serials = FindObjectsOfType<MicroSerial>().ToList();
    }

    private void Update()
    {
        if (!setupComplete)
        {
            foreach (MicroSerial serial in serials)
            {
                switch (serial.Micro)
                {
                    case MICRO.NOT_FOUND:
                        break;
                    case MICRO.SC_MOTORS:
                        sc = serial;
                        break;
                    case MICRO.ST_MOTORS:
                        st = serial;
                        break;
                    case MICRO.XIAOMI_MOTORS:
                        xiaomi = serial;
                        break;
                    default:
                        break;
                }
            }

            setupComplete = sc && st && xiaomi;
        }
    }

    public bool SendSerialMessage(MICRO micro, string msg)
    {
        MicroSerial serial = micro switch
        {
            MICRO.NOT_FOUND => null,
            MICRO.SC_MOTORS => sc,
            MICRO.ST_MOTORS => st,
            MICRO.XIAOMI_MOTORS => xiaomi,
            _ => null,
        };

        return serial && serial.SendSerialMessage(msg);
    }
}
