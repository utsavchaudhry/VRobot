using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshqarApps.DynamicJoint
{
    public class DynamicJointLimitSwingTwist : DynamicJointLimit
    {
        [Header("Swing Twist Limits")]
        [Range(0f, 180f)] public float twistLimit = 45;

        [Range(0f, 1f)] public float limitWidthRatio = 1;
        [Range(0f, 1f)] public float limitHeightRatio = 1;

        public Quaternion LimitSwing(Quaternion rotation, bool visualizeMode = false)
        {
            if (this.mainAxis == Vector3.zero) return rotation; // Ignore with zero axes
                                                                //if (rotation == Quaternion.identity) return rotation; // Assuming initial rotation is in the reachable area

            Quaternion offsetRot = Quaternion.Euler(limitsOffset);
            if (visualizeMode)
            {
                offsetRot = Quaternion.identity;
            }

            Vector3 offsetSwingAxis = (offsetRot * this.mainAxis).normalized;
            Vector3 offsetSecondaryAxis = (offsetRot * this.secondaryAxis).normalized;
            Vector3.OrthoNormalize(ref offsetSwingAxis, ref offsetSecondaryAxis);
            Vector3 offsetCrossAxis = Vector3.Cross(offsetSwingAxis, offsetSecondaryAxis);

            Vector3 targetSwing = rotation * mainAxis;

            float angle = Vector3.SignedAngle(offsetSwingAxis, targetSwing, Vector3.Cross(offsetSwingAxis, targetSwing));
            Vector3 unlimitedTargetSwing = targetSwing;

            float maxAngle = angle > 0 ? limitAngle : -limitAngle;
            Quaternion maxRotation = Quaternion.AngleAxis(maxAngle, Vector3.Cross(offsetSwingAxis, targetSwing));

            Vector3 maxTargetSwing = maxRotation * offsetSwingAxis;

            // Scale Limit Based on Width Height Ratios
            Vector3 limitCircleCenter = Vector3.ProjectOnPlane(offsetSwingAxis - maxTargetSwing, offsetSwingAxis) + maxTargetSwing;
            float circleRadius = Vector3.Distance(maxTargetSwing, limitCircleCenter);
            Debug.DrawLine(transform.position + Direction(maxTargetSwing), transform.position + Direction(maxTargetSwing));

            Vector3 projectToCircle = (maxTargetSwing - limitCircleCenter);
            float width = circleRadius * limitWidthRatio;
            float height = circleRadius * limitHeightRatio;

            Vector3 coneX = Vector3.Project(projectToCircle, offsetSecondaryAxis);
            Vector3 coneY = Vector3.Project(projectToCircle, offsetCrossAxis);

            if (limitHeightRatio < 1 || limitWidthRatio < 1)
            {
                //find cartesian ellipse coordinates intersection
                Vector2 center = Vector2.zero;
                Vector2 pt = new Vector2(coneX.magnitude, coneY.magnitude);
                Vector3[] candidateProjectionPoints = FindEllipseSegmentIntersections(width, height, center, pt, true);

                //map cartesian intersection point back to joint space
                if (candidateProjectionPoints.Length > 0)
                {
                    Vector3 projectedX = coneX.normalized * candidateProjectionPoints[0].x;
                    Vector3 projectedY = coneY.normalized * candidateProjectionPoints[0].y;
                    maxTargetSwing = (limitCircleCenter + projectedX + projectedY).normalized;
                }
            }

            if (Vector3.Angle(offsetSwingAxis, maxTargetSwing) < Vector3.Angle(offsetSwingAxis, targetSwing))
            {
                isClampedToLimit = true;
                targetSwing = maxTargetSwing;
            }

            // Get the limited swing axis
            Quaternion limitedSwingRotation = Quaternion.FromToRotation(offsetSwingAxis, targetSwing);

            // Rotation from current(illegal) swing rotation to the limited(legal) swing rotation
            Quaternion toLimits = Quaternion.FromToRotation(unlimitedTargetSwing, limitedSwingRotation * offsetSwingAxis);

            // Subtract the illegal rotation
            return toLimits * rotation;

        }

        public override Vector3 GetStretchAxis()
        {
            return GetMainAxisWorld();
        }

        public override Vector3 GetMidVectorWorld()
        {
            return GetMainAxisWorld();
        }

        public override Vector3 GetMainAxisWorld()
        {
            Quaternion offsetRot = Quaternion.Euler(limitsOffset);
            Vector3 offsetSwingAxis = (offsetRot * this.mainAxis).normalized;
            return Direction(offsetSwingAxis);
        }

        public override Quaternion LimitRotation(Quaternion rotation, float jointLimitStrength = 1)
        {
            Quaternion swing = LimitSwing(rotation);
            return LimitTwist(swing, mainAxis, secondaryAxis, twistLimit);
        }



        // Find the points of intersection between
        // an ellipse and a line segment.
        public Vector3[] FindEllipseSegmentIntersections(
                float width, float height, Vector3 pt1, Vector3 pt2, bool segment_only)
        {
            // If the ellipse or line segment are empty, return no intersections.
            if ((width == 0) || (height == 0) ||
                ((pt1.x == pt2.x) && (pt1.y == pt2.y)))
                return new Vector3[] { };

            // Get the semimajor and semiminor axes.
            float a = width;
            float b = height;

            // Calculate the quadratic parameters.
            float A = (pt2.x - pt1.x) * (pt2.x - pt1.x) / a / a +
                      (pt2.y - pt1.y) * (pt2.y - pt1.y) / b / b;
            float B = 2 * pt1.x * (pt2.x - pt1.x) / a / a +
                      2 * pt1.y * (pt2.y - pt1.y) / b / b;
            float C = pt1.x * pt1.x / a / a + pt1.y * pt1.y / b / b - 1;

            // Make a list of t values.
            List<float> t_values = new List<float>();

            // Calculate the discriminant.
            float discriminant = B * B - 4 * A * C;
            if (discriminant == 0)
            {
                // One real solution.
                t_values.Add(-B / 2 / A);
            }
            else if (discriminant > 0)
            {
                // Two real solutions.
                t_values.Add((float)((-B + Mathf.Sqrt(discriminant)) / 2 / A));
                t_values.Add((float)((-B - Mathf.Sqrt(discriminant)) / 2 / A));
            }

            // Convert the t values into points.
            List<Vector3> points = new List<Vector3>();
            foreach (float t in t_values)
            {
                // If the points are on the segment (or we
                // don't care if they are), add them to the list.
                if (!segment_only || ((t >= 0f) && (t <= 1f)))
                {
                    float x = pt1.x + (pt2.x - pt1.x) * t;
                    float y = pt1.y + (pt2.y - pt1.y) * t;
                    points.Add(new Vector3(x, y));
                }
            }

            // Return the points.
            return points.ToArray();
        }

        public override bool Apply(float jointLimitStrength = 1)
        {
            bool hasChanged = base.Apply(jointLimitStrength);
            return isClampedToLimit;
        }

    }
}
