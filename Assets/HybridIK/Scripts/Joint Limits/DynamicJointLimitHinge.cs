using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshqarApps.DynamicJoint
{
    public class DynamicJointLimitHinge : DynamicJointLimit
    {
        [Header("Hinge Limits")]
        [Range(0f, 360f)]
        public float hingeAngleOffset = 0;

        public Vector3 cross = Vector3.up;

        private Quaternion lastRotation = Quaternion.identity;

        public override Vector3 GetMainAxisWorld()
        {
            Quaternion offsetRot = Quaternion.Euler(limitsOffset);
            Vector3 offsetSwingAxis = (offsetRot * this.mainAxis).normalized;
            return Direction(offsetSwingAxis);
        }

        public override Vector3 GetStretchAxis()
        {
            return GetMainAxisWorld();
        }

        public override Vector3 GetMidVectorWorld()
        {
            return GetPlaneAxisWorld();
        }

        public Vector3 GetPlaneAxisWorld()
        {
            Quaternion offsetRot = Quaternion.Euler(limitsOffset);
            Vector3 swing = Direction(offsetRot * mainAxis.normalized);

            Vector3 secondaryAxis = new Vector3(mainAxis.y, mainAxis.z, mainAxis.x);
            Vector3 cross = Direction(Vector3.Cross(mainAxis, secondaryAxis));
            cross = Direction(offsetRot * this.cross);
            Quaternion hingeOffset = Quaternion.AngleAxis(hingeAngleOffset, swing);
            return hingeOffset * cross;
        }

        /*
        * Apply the hinge rotation limit
        * */
        public Quaternion LimitHinge(Quaternion rotation)
        {
            Quaternion offsetRot = Quaternion.Euler(limitsOffset);

            Vector3 offsetSwingAxis = (offsetRot * this.mainAxis);
            Vector3 offsetSecondaryAxis = (offsetRot * this.cross);
            Vector3.OrthoNormalize(ref offsetSwingAxis, ref offsetSecondaryAxis);
            Vector3 offsetCrossAxis = Vector3.Cross(offsetSwingAxis, offsetSecondaryAxis);

            Quaternion hingeOffset = Quaternion.AngleAxis(hingeAngleOffset, offsetSwingAxis);
            Quaternion minRotation = Quaternion.AngleAxis(-limitAngle, offsetSwingAxis) * hingeOffset;
            Quaternion maxRotation = Quaternion.AngleAxis(limitAngle, offsetSwingAxis) * hingeOffset;

            // Get 1 degree of freedom rotation along axis
            Quaternion free1DOFTarget = Quaternion.FromToRotation(rotation * offsetSwingAxis, offsetSwingAxis) * rotation;

            if (limitAngle >= 180)
                return free1DOFTarget;

            float midLimit = (limitAngle >= 90f) ? 180 - limitAngle : limitAngle;
            Quaternion free1DOFMid = Quaternion.RotateTowards(minRotation, maxRotation, midLimit);
            if (limitAngle >= 90f)
            {
                Quaternion flip180 = Quaternion.AngleAxis(180, offsetSwingAxis);
                free1DOFMid *= flip180;
            }

            Quaternion lastRotation = free1DOFTarget;

            float angle = Quaternion.Angle(free1DOFTarget, free1DOFMid);
            Quaternion clampedFree1DOF = Quaternion.RotateTowards(free1DOFMid, free1DOFTarget, limitAngle);

            isClampedToLimit = angle >= limitAngle;

            return clampedFree1DOF;

        }


        public override Quaternion LimitRotation(Quaternion rotation, float jointLimitStrength = 1)
        {
            lastRotation = LimitHinge(rotation);
            return lastRotation;
        }

        public override bool Apply(float jointLimitStrength = 1)
        {
            bool hasChanged = base.Apply(jointLimitStrength);
            return isClampedToLimit;
        }

    }
}