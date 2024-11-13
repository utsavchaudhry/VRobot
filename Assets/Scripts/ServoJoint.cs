using UnityEngine;
using System.Collections;

public class ServoJoint : MonoBehaviour
{
    public int Signal { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] [Range(-360f, 360f)] private float startAngle;
    [SerializeField] [Range(-360f, 360f)] private float offset;
    [SerializeField] [Range(0, 360)] private int range = 180;
    [SerializeField] private int minPWM = 100;
    [SerializeField] private int maxPWM = 600;
    private enum Axis { X, Y, Z }
    [SerializeField] private Axis axis;
    [SerializeField] private bool flip;

    private float lastPwm = -1f;
    private float angle;

    private void Start()
    {
        _ = StartCoroutine(CalculateSignal());
    }

    private IEnumerator CalculateSignal()
    {
        lastPwm = -1f;

        if (!target)
        {
            target = transform;
        }

        while (true)
        {
            angle = axis switch
            {
                Axis.X => target.localEulerAngles.x,
                Axis.Y => target.localEulerAngles.y,
                Axis.Z => target.localEulerAngles.z,
                _ => target.localEulerAngles.z,
            };

            if (startAngle < 0f && angle >= 180f)
            {
                angle -= 360f;
            }

            angle = Mathf.Clamp(angle + offset, startAngle, startAngle + range);

            float pwm01 = Mathf.Clamp01((angle - startAngle) / range);

            if (flip)
            {
                pwm01 = 1f - pwm01;
            }

            if (lastPwm < 0f || (Mathf.Abs(pwm01 - lastPwm) <= 0.25f && pwm01 != lastPwm))
            {
                Signal = Mathf.RoundToInt(minPWM + ((maxPWM - minPWM) * pwm01));
            }

            lastPwm = pwm01;

            yield return null;
        }
    }
}
