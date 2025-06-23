using System.Collections;
using UnityEngine;
using TMPro;

public class Clamp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI btnText;
    [SerializeField] private int motorID = 10;
    [SerializeField] private int minPWM = 0;
    [SerializeField] private int maxPWM = 512;
    [SerializeField] private bool flip;
    private enum Side { Left, Right }
    [SerializeField] private Side side;
    private enum Mode { VR, PC, Mobile }
    [SerializeField] private Mode mode;

    private float input;
    private int signal;

    private void Start()
    {
        _ = StartCoroutine(CalculateSignal());
        UpdateBtnText();
    }

    private void UpdateBtnText()
    {
        if (!btnText)
        {
            return;
        }

        btnText.text = (input == 0f ? "Close " : "Open ") + side.ToString() + " Gripper";
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

    public void Toggle()
    {
        input = input == 0f ? 1f : 0f;
        UpdateBtnText();
    }

    private IEnumerator CalculateSignal()
    {
        while (true)
        {
            switch (mode)
            {
                case Mode.VR:
                    input = side == Side.Left ? InputManager.LeftController.Trigger : InputManager.RightController.Trigger;
                    break;
                case Mode.PC:
                    input = Input.GetMouseButton(side == Side.Left ? 0 : 1) ? 1f : 0f;
                    break;
                case Mode.Mobile:
                    break;
                default:
                    break;
            }

            signal = Mathf.RoundToInt(minPWM + ((flip ? (1f - input) : input) * (maxPWM - minPWM)));
            

            yield return null;
        }
    }
}
