using System;
using UnityEngine;

public class ForwardKinematics : MonoBehaviour
{
    private enum Axis { Pitch, Yaw, Roll }

    [SerializeField] private float a1y = 650;
    [SerializeField] private float a2x = 400;
    [SerializeField] private float a2y = 680;
    [SerializeField] private float a3y = 1100;
    [SerializeField] private float a4y = 230;
    [SerializeField] private float a4x = 766;
    [SerializeField] private float a5x = 345;
    [SerializeField] private float a6x = 244;

    [Space]

    [SerializeField] private float j1;
    [SerializeField] private float j2;
    [SerializeField] private float j3;
    [SerializeField] private float j4;
    [SerializeField] private float j5;
    [SerializeField] private float j6;

    private void OnValidate()
    {
        Compute();
    }

    private void Compute()
    {
        Matrix4x4 r1 = GetRotationMatrix(Axis.Yaw, j1);
        Matrix4x4 r2 = GetRotationMatrix(Axis.Roll, j2);
        Matrix4x4 r3 = GetRotationMatrix(Axis.Roll, j3);
        Matrix4x4 r4 = GetRotationMatrix(Axis.Pitch, j4);
        Matrix4x4 r5 = GetRotationMatrix(Axis.Roll, j5);
        Matrix4x4 r6 = GetRotationMatrix(Axis.Pitch, j6);

        Matrix4x4 t1 = r1;
        t1.m13 = a1y;

        Matrix4x4 t2 = r2;
        t2.m03 = a2x;
        t2.m13 = a2y;

        Matrix4x4 t3 = r3;
        t3.m13 = a3y;

        Matrix4x4 t4 = r4;
        t4.m03 = a4x;
        t4.m13 = a4y;

        Matrix4x4 t5 = r5;
        t5.m03 = a5x;

        Matrix4x4 t6 = r6;
        t6.m03 = a6x;

        Matrix4x4 t16 = t1 * t2 * t3 * t4 * t5 * t6;

        Vector4 tcp_local = new(0f, 0f, 0f, 1f);
        Vector4 tcp_base = t16 * tcp_local;

        Matrix4x4 arm_rotation = r1 * r2 * r3;
        Matrix4x4 wrist_rotation = r4 * r5 * r6;

        Matrix4x4 tcp_rotation = arm_rotation * wrist_rotation;

        Debug.Log("TCP Position: " +
            RoundToDecimalPlaces(tcp_base.x) + "," + RoundToDecimalPlaces(tcp_base.y) + "," + RoundToDecimalPlaces(tcp_base.z)
            + " | TCP EulerAngles: " + RoundToDecimalPlaces(Mathf.Atan2(tcp_rotation.m22, tcp_rotation.m21) * Mathf.Rad2Deg) + "," +
            RoundToDecimalPlaces(Mathf.Atan2(Mathf.Sqrt(1f - Mathf.Pow(tcp_rotation.m20, 2)), -tcp_rotation.m20) * Mathf.Rad2Deg) + "," +
            RoundToDecimalPlaces(Mathf.Atan2(tcp_rotation.m00, tcp_rotation.m10) * Mathf.Rad2Deg));
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
