using System.Collections;
using UnityEngine;

public class Clamp : MonoBehaviour
{
    [SerializeField] private int motorID = 10;
    [SerializeField] private int minPWM = 0;
    [SerializeField] private int maxPWM = 512;
    [SerializeField] private bool flip;
    [SerializeField] private bool pcMode;
    private enum Side { Left, Right }
    [SerializeField] private Side side;

    private float input;
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

    public float GetCurrentInput()
    {
        return input;
    }

    private IEnumerator CalculateSignal()
    {
        while (true)
        {
            input = pcMode
                ? Input.GetMouseButton(side == Side.Left ? 0 : 1) ? 1f : 0f
                : side == Side.Left ? InputManager.LeftController.Trigger : InputManager.RightController.Trigger;

            signal = Mathf.RoundToInt(minPWM + ((flip ? (1f - input) : input) * (maxPWM - minPWM)));
            

            yield return null;
        }
    }
}
