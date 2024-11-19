using System.Collections;
using UnityEngine;

public class Clamp : MonoBehaviour
{
    [SerializeField] private int motorID = 10;
    [SerializeField] private int minPWM = 0;
    [SerializeField] private int maxPWM = 512;
    [SerializeField] private bool flip;

    private int lastSignal = -69420;

    private void Start()
    {
        _ = StartCoroutine(CalculateSignal());
    }

    private IEnumerator CalculateSignal()
    {
        while (true)
        {
            float input = InputManager.LeftController.Trigger;
            if (flip)
            {
                input = 1f - input;
            } 
            int currentSignal = Mathf.RoundToInt(minPWM + (input * (maxPWM - minPWM)));
            if (Mathf.Abs(currentSignal - lastSignal) >= 15f)
            {
                if (SerialHandler.SendSerialData(motorID + "," + currentSignal))
                {
                    lastSignal = currentSignal;
                }
            }

            yield return null;
        }
    }
}
