using System.Collections;
using UnityEngine;

public class Clamp : MonoBehaviour
{
    [SerializeField] private int motorID = 10;
    [SerializeField] private int minPWM = 0;
    [SerializeField] private int maxPWM = 512;
    [SerializeField] private bool flip;

    private int signal;

    private void Start()
    {
        _ = StartCoroutine(CalculateSignal());
    }

    public int GetMotorID()
    {
        return motorID;
    }

    public int GetCurrentSignal()
    {
        return signal;
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
            
            signal = Mathf.RoundToInt(minPWM + (input * (maxPWM - minPWM)));
            

            yield return null;
        }
    }
}
