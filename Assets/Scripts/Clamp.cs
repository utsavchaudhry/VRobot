using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Clamp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI btnText;
    [SerializeField] private Image btnImage;
    [SerializeField] private int motorID = 10;
    [SerializeField] private int minPWM = 0;
    [SerializeField] private int maxPWM = 512;
    [SerializeField] private bool flip;
    private enum Side { Left, Right }
    [SerializeField] private Side side;
    private enum Mode { VR, PC, Mobile }
    [SerializeField] private Mode mode;

    private Color openColor;
    private Color closedColor;
    private float input;
    private int signal;

    private void Start()
    {
        if (btnImage)
        {
            openColor = btnImage.color;
        }
        closedColor = new Color(0f, 0.749f, 1f, Mathf.Clamp01(openColor.a * 3f));

        _ = StartCoroutine(CalculateSignal());
        UpdateBtnText();
    }

    private void UpdateBtnText()
    {
        if (btnText)
        {
            btnText.text = (input == 0f ? "Close\n" : "Open\n") + side.ToString() + " Gripper";
        }

        if (btnImage)
        {
            _ = btnImage.DOKill();
            _ = btnImage.DOColor(input == 0f ? openColor : closedColor, 0.15f);
        }
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
