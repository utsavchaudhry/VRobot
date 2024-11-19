using System.Collections;
using UnityEngine;

public class ServoOffset : MonoBehaviour
{
    public int Offset { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] [Range(0f, 180f)] private float maxAngle = 90f;
    [SerializeField] private int maxPWM = 1024;
    [SerializeField] private Axis axis;
    [SerializeField] private bool flip;

    private float angle;

    private void Start()
    {
        _ = StartCoroutine(CalculateOffset());
    }

    private IEnumerator CalculateOffset()
    {
        if (!target)
        {
            target = transform;
        }

        while (true)
        {
            angle = axis switch
            {
                Axis.X => target.eulerAngles.x,
                Axis.Y => target.eulerAngles.y,
                Axis.Z => target.eulerAngles.z,
                _ => target.eulerAngles.z,
            };

            if (angle > 180f)
            {
                angle -= 360f;
            }

            Offset = Mathf.RoundToInt(Mathf.Clamp(angle, -maxAngle, maxAngle) / maxAngle * maxPWM * (flip ? -1f : 1f));

            yield return null;
        }
    }
}
