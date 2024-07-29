using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AshqarApps.DynamicJoint;

[System.Serializable]
public class HybridIKJoint
{
    public Transform jointTransform;
    public Quaternion initialRotation;
    public Vector3 initialPosition;
    public Vector3 initialPositionWorld;
    public Vector3 previousPositionWorld;

    public Quaternion cachedInitialRotation;
    public Vector3 cachedLocalPosition;


    public Vector3 targetPosition;
    public Vector3 targetLocalPosition;
    public Vector3 stretchedTargetPosition;
    public bool isStretchNode;

    private Vector3 ikTargetPosition;

    public bool IsEndNode = false;

    public HybridIKJoint child = null;
    public HybridIKJoint parent = null;

    [HideInInspector]
    public int childIndex = -1;
    [HideInInspector]
    public int parentIndex = -1;

    public float boneLength;
    public Vector3 toParent;

    public bool oldVecInitialized;
    public Vector3 oldVec;
    public Quaternion rotationIK;

    public float jointRadius;

    public DynamicJointLimit jointLimit;

    public bool isPositionConstrained = false;
    public bool isRotationConstrained = false;

    public bool overrideConstraint;
    public HybridIKConstraint constraint;
    public Transform targetIKSpace = null;

    public bool enableKeyframeConstraints = true;

    public Vector3 cachedPosition = Vector3.zero;
    public Quaternion cachedRotation = Quaternion.identity;
    public Quaternion targetRotation = Quaternion.identity;
    public List<Quaternion> keyedRotations;
    public List<Vector3> keyedPositions;
    public List<Vector3> keyedLocalPositions;

    public Quaternion zeroRotation = Quaternion.identity;
    public Vector3 zeroPosition = Vector3.zero;

    public Quaternion lastFrameRotation = Quaternion.identity;
    public Vector3 lastFramePosition = Vector3.zero;
    public float currentAngularVelocity = 0;
    public float currentStretchVelocity = 0;

    public bool enableStretch = false;
    public float inwardsStretch = 0;
    public float outwardsStretch = 0;
    public Vector3 stretchAxis = Vector3.forward;
    public HybridIKNodeStretchLimits stretchLimits;

    // Angular Motion
    public bool overrideAngularMotion = false;
    public float maxAngularMotionVelocity = 30f;
    public float angularMotionAcceleration = 5f;

    public void ResetToZeroTransforms()
    {
        if (parent != null)
            parent.jointTransform.localRotation = parent.zeroRotation;
        this.jointTransform.localPosition = zeroPosition;
    }

    public bool HasIKConstraint()
    {
        return ((enableKeyframeConstraints || overrideConstraint)) && constraint != null && constraint.jointTransform != null && (constraint.constrainPosition || constraint.constrainRotation);
    }

    public bool HasPositionConstraint()
    {
        return (enableKeyframeConstraints || overrideConstraint) && constraint != null && constraint.jointTransform != null && constraint.constrainPosition;
    }

    public bool HasRotationConstraint()
    {
        return (enableKeyframeConstraints || overrideConstraint) && constraint != null && constraint.jointTransform != null && constraint.constrainRotation;
    }

    public HybridIKJoint(Transform t, HybridIKJoint child, float radius = 0.1f)
    {
        if (child != null)
        {
            child.boneLength = (t.position - child.jointTransform.position).magnitude;
            child.parent = this;
            this.child = child;
        }

        lastFrameRotation = t.localRotation;
        initialPosition = t.localPosition;
        initialRotation = t.localRotation;
        initialPositionWorld = t.position;
        targetPosition = t.position;

        previousPositionWorld = initialPositionWorld;

        jointTransform = t;

        jointRadius = radius;

        oldVec = Vector3.up;

        keyedRotations = new List<Quaternion>();
        keyedPositions = new List<Vector3>();
        keyedLocalPositions = new List<Vector3>();

        this.jointLimit = t.gameObject.GetComponent<DynamicJointLimit>();
    }

    public Vector3 GetCurrentPositionWorld()
    {
        return jointTransform.position;
    }

    public Vector3 GetInitialPositionWorld()
    {
        return initialPositionWorld;
    }
}

[System.Serializable]
public class HybridIKNodeStretchLimits
{
    public Transform targetSpace;

    public Vector3 minStretchLimits;
    public Vector3 maxStretchLimits;

    public float maxForward = 0;
    public float maxBackward = 0;

    public float maxLeft = 0;
    public float maxRight = 0;

    public float maxDown = 0;
    public float maxUp = 0;

    public Vector3 ApplyStretchLimit(Vector3 targetVec, Vector3 localOrigin)
    {
        if (targetSpace == null)
            return Vector3.zero;

        Vector3 currentPos = targetSpace.position;
        targetSpace.localPosition = localOrigin;
        Vector3 diff = currentPos - targetSpace.position;
        targetVec += diff;

        Vector3 vec = targetSpace.InverseTransformDirection(targetVec);
        vec.x = Mathf.Clamp(vec.x, minStretchLimits.x, maxStretchLimits.x);
        vec.y = Mathf.Clamp(vec.y, minStretchLimits.y, maxStretchLimits.y);
        vec.z = Mathf.Clamp(vec.z, minStretchLimits.z, maxStretchLimits.z);

        vec = targetSpace.TransformDirection(vec);

        return vec;
    }
}