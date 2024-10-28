using System;
using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
    private enum Axis { Pitch, Yaw, Roll }

    [SerializeField] private Transform target;

    [Space]

    [SerializeField] private float a1y = 650;
    [SerializeField] private float a2x = 400;
    [SerializeField] private float a2y = 680;
    [SerializeField] private float a3y = 1100;
    [SerializeField] private float a4y = 230;
    [SerializeField] private float a4x = 766;
    [SerializeField] private float a5x = 345;
    [SerializeField] private float a6x = 244;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Compute();
        }
    }

    private void Compute()
    {
        #region Decouple Wrist from arm

        Matrix4x4 tcp_rotation = GetRotationMatrix(Axis.Roll, target.eulerAngles.z) *
            GetRotationMatrix(Axis.Yaw, target.eulerAngles.y) *
            GetRotationMatrix(Axis.Pitch, target.eulerAngles.x);

        Vector3 tcp_axis_x = new(tcp_rotation.m00, tcp_rotation.m10, tcp_rotation.m20);

        Vector3 wrist_position = target.position - (a6x * tcp_axis_x);

        Debug.Log("Wrist Position: " + RoundToDecimalPlaces(wrist_position.x) + "," +
            RoundToDecimalPlaces(wrist_position.y) + "," +
            RoundToDecimalPlaces(wrist_position.z));

        #endregion

        #region J1

        float j1 = Mathf.Atan2(wrist_position.z, wrist_position.x);

        #endregion

        #region J2,J2

        float wrist_xz = Mathf.Sqrt((wrist_position.x * wrist_position.x) + (wrist_position.z * wrist_position.z));
        float l = wrist_xz - a2x;
        float h = wrist_position.y - a1y - a2y;
        float p = Mathf.Sqrt((h * h) + (l * l));

        float b4x = Mathf.Sqrt((a4y * a4y) + Mathf.Pow(a4x + a5x, 2));
        float alpha = Mathf.Atan2(h, l);
        float cos_beta = ((p * p) + (a3y * a3y) - (b4x * b4x)) / (2 * p * a3y);
        float beta = Mathf.Atan2(Mathf.Sqrt(1f - (cos_beta * cos_beta)), cos_beta);

        float j2 = (Mathf.PI / 2f) - alpha - beta;

        float cos_gamma = ((a3y * a3y) + (b4x * b4x) - (p * p)) / (2 * a3y * b4x);
        float gamma = Mathf.Atan2(Mathf.Sqrt(1f - (cos_gamma * cos_gamma)), cos_gamma);
        float delta = Mathf.Atan2(a4x + a5x, a4y);

        float j3 = Mathf.PI - gamma - delta;

        #endregion

        Debug.Log(RoundToDecimalPlaces(j1 * Mathf.Rad2Deg) + "," +
            RoundToDecimalPlaces(j2 * Mathf.Rad2Deg) + "," +
            RoundToDecimalPlaces(j3 * Mathf.Rad2Deg));
    }

    private Matrix4x4 GetRotationMatrix(Axis axis, float angle)
    {
        angle *= Mathf.Deg2Rad;

        Matrix4x4 rotationMatrix = Matrix4x4.identity;

        switch (axis)
        {
            case Axis.Pitch:

                rotationMatrix.m11 = Mathf.Cos(angle);
                rotationMatrix.m12 = -Mathf.Sin(angle);
                rotationMatrix.m21 = Mathf.Sin(angle);
                rotationMatrix.m22 = Mathf.Cos(angle);

                break;

            case Axis.Yaw:

                rotationMatrix.m00 = Mathf.Cos(angle);
                rotationMatrix.m02 = Mathf.Sin(angle);
                rotationMatrix.m20 = -Mathf.Sin(angle);
                rotationMatrix.m22 = Mathf.Cos(angle);

                break;

            case Axis.Roll:

                rotationMatrix.m00 = Mathf.Cos(angle);
                rotationMatrix.m01 = -Mathf.Sin(angle);
                rotationMatrix.m10 = Mathf.Sin(angle);
                rotationMatrix.m11 = Mathf.Cos(angle);

                break;

            default:
                break;
        }

        return rotationMatrix;
    }

    private float RoundToDecimalPlaces(float number, int decimalPlaces = 1)
    {
        return (float)Math.Round(number, decimalPlaces, MidpointRounding.AwayFromZero);
    }
}
