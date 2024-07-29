using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshqarApps.DynamicJoint
{
    public class DynamicJointLimit : MonoBehaviour
    {
        public bool enable = true;

        [Header("Main")]
        public Vector3 mainAxis = Vector3.forward;

        [HideInInspector]
        public Vector3 secondaryAxis = Vector3.up;
        [HideInInspector]
        public Vector3 crossAxis { get { return Vector3.Cross(mainAxis, secondaryAxis); } }

        [HideInInspector] public Quaternion zeroRotation;
        public bool zeroRotationOverride { get; private set; }

        public Vector3 limitsOffset;

        private bool initialized;
        private bool applicationQuit;
        private bool zeroRotationSet;

        protected bool isClampedToLimit = false;

        /// <summary>
        /// The swing limit.
        /// </summary>
        [Range(0f, 180f)] public float limitAngle = 45;

        public bool applyEveryFrame = false;

        //[Header("Dynamics")]

        [HideInInspector]
        public float bounce = 0;
        [HideInInspector]
        public float friction = 0;

        /*
             * Applies uniform twist limit to the rotation
             * */
        protected Quaternion LimitTwist(Quaternion rotation, Vector3 axis, Vector3 orthoAxis, float twistLimit)
        {
            twistLimit = Mathf.Clamp(twistLimit, 0, 180);
            if (twistLimit >= 180) return rotation;

            Vector3 normal = rotation * axis;
            Vector3 orthoTangent = orthoAxis;
            Vector3.OrthoNormalize(ref normal, ref orthoTangent);

            Vector3 rotatedOrthoTangent = rotation * orthoAxis;
            Vector3.OrthoNormalize(ref normal, ref rotatedOrthoTangent);

            Quaternion fixedRotation = Quaternion.FromToRotation(rotatedOrthoTangent, orthoTangent) * rotation;

            if (twistLimit <= 0) return fixedRotation;

            float twistAngle = Quaternion.Angle(fixedRotation, rotation);
            if (twistAngle >= twistLimit)
            {
                isClampedToLimit = true;
            }

            // Rotate from zero twist to free twist by the limited angle
            return Quaternion.RotateTowards(fixedRotation, rotation, twistLimit);
        }

        public virtual Vector3 GetStretchAxis()
        {
            return Vector3.zero;
        }

        public virtual Vector3 GetMainAxisWorld()
        {
            return Direction(mainAxis);
        }

        public virtual Quaternion LimitRotation(Quaternion rotation, float jointLimitStrength = 1)
        {
            return rotation;
        }

        public virtual Vector3 GetMidVectorWorld()
        {
            return mainAxis;
        }

        protected Vector3 Direction(Vector3 v)
        {
            if (transform.parent == null) return zeroRotation * v;
            return transform.parent.rotation * (zeroRotation * v);
        }

        /// <summary>
        /// Returns the limited local rotation.
        /// </summary>
        public Quaternion GetLimitedLocalRotation(Quaternion localRotation, out bool changed, float jointLimitStrength = 1)
        {
            // Making sure the Rotation Limit is initiated
            if (!initialized) Awake();

            // Subtracting defaultLocalRotation
            Quaternion rotation = Quaternion.Inverse(zeroRotation) * localRotation;

            Quaternion limitedRotation = LimitRotation(rotation);
#if UNITY_2018_3_OR_NEWER
            limitedRotation = Quaternion.Normalize(limitedRotation);
#endif
            changed = limitedRotation != rotation;

            if (!changed) return localRotation;

            // Apply defaultLocalRotation back on
            return zeroRotation * limitedRotation;
        }

        public float EvaluateSwingLimitAtOrthoAngle(float angle)
        {
            float sinAngle = Mathf.Abs(Mathf.Sin(Mathf.Deg2Rad * angle));
            //return sinAngle;
            return (sinAngle * limitAngle);
        }

        /// <summary>
        /// Apply the rotation limit to transform.localRotation. Returns true if the limit has changed the rotation.
        /// </summary>
        public virtual bool Apply(float jointLimitStrength = 1)
        {
            isClampedToLimit = false;

            if (jointLimitStrength == 0 || !enable)
                return false;

            bool changed = false;

            Quaternion targetRot = GetLimitedLocalRotation(transform.localRotation, out changed);

            if (changed)
            {
                if (jointLimitStrength < 1)
                {
                    float angle = Quaternion.Angle(transform.localRotation, targetRot);

                    Quaternion easedRotation = Quaternion.RotateTowards(transform.localRotation, targetRot, angle * jointLimitStrength);

                    transform.localRotation = easedRotation;
                }
                else
                {
                    transform.localRotation = targetRot;
                }
            }

            return changed;
        }

        /// <summary>
        /// Disable this instance making sure it is initiated. Use this if you intend to manually control the updating of this Rotation Limit.
        /// </summary>
        public void Disable()
        {
            if (initialized)
            {
                enabled = false;
                return;
            }

            Awake();
            enabled = false;
        }

        public void SetZeroRotation()
        {
            zeroRotation = transform.localRotation;
            zeroRotationSet = true;
            zeroRotationOverride = false;
        }

        /// <summary>
        /// Map the zero rotation point to the specified rotation.
        /// </summary>
        public void SetZeroRotation(Quaternion localRotation)
        {
            zeroRotation = localRotation;
            zeroRotationSet = true;
            zeroRotationOverride = true;
        }

        /*
     * Returns the angle between two vectors on a plane with the specified normal
     * */
        public static float GetOrthogonalAngle(Vector3 v1, Vector3 v2, Vector3 normal)
        {
            Vector3.OrthoNormalize(ref normal, ref v1);
            Vector3.OrthoNormalize(ref normal, ref v2);
            return Vector3.Angle(v1, v2);
        }

        void LateUpdate()
        {
            if (applyEveryFrame)
                Apply();
        }

        void Awake()
        {
            // Store the local rotation to map the zero rotation point to the current rotation
            if (!zeroRotationSet) SetZeroRotation();

            if (mainAxis == Vector3.zero)
            {
                Debug.LogError("Axis is Vector3.zero.");
                mainAxis = Vector3.forward;
            }
            initialized = true;
        }
    }
}