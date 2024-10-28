using UnityEngine;

public class ServoJoint : MonoBehaviour
{
    public int Signal { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] [Range(-180f, 180f)] private float startAngle;
    [SerializeField] [Range(-180f, 180f)] private float offset;
    [SerializeField] [Range(0, 360)] private int range = 180;
    [SerializeField] private int minPWM = 100;
    [SerializeField] private int maxPWM = 600;
    private enum Axis { X, Y, Z }
    [SerializeField] private Axis axis;
    [SerializeField] private bool flip;

    private float angle;

    private void Start()
    {
        if (!target)
        {
            target = transform;
        }
    }

    private void Update()
    {
        angle = axis switch
        {
            Axis.X => transform.localEulerAngles.x,
            Axis.Y => transform.localEulerAngles.y,
            Axis.Z => transform.localEulerAngles.z,
            _ => transform.localEulerAngles.z,
        };

        if (angle > 180f)
        {
            angle -= 360f;
        }

        int pwm = Mathf.RoundToInt(minPWM + ((maxPWM - minPWM) *
            (Mathf.Clamp(angle + offset, startAngle, startAngle + range) - startAngle) / range));

        if (flip)
        {
            pwm = maxPWM + minPWM - Signal;
        }

        Signal = pwm;
    }
}
