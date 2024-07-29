using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AshqarApps.DynamicJoint;

public enum InitializePoseFrom
{
    LAST_FRAME,
    INITIAL_POSE,
    SOURCE_ANIMATION
}

public enum IKMode
{
    CCD,
    FABRIK,
    CCD_PLUS_FABRIK,
}

public class HybridInverseKinematicsNode : MonoBehaviour
{
    [Header("IK Chain Definition")]
    public Transform rootNode;
    public Transform endNode;
    public InitializePoseFrom initializePoseFrom = InitializePoseFrom.LAST_FRAME;
    public float nodeRadius = 0.1f;

    [Header("IK Target")]
    public Transform targetTransform;
    private Vector3 targetPosition;

    [Header("Mode")]
    public IKMode mode = IKMode.CCD;
    public int numIterations = 2;

    [Header("End Orientation")]
    public bool constrainEndOrientation = false;
    public bool constrainInGlobalSpace = false;

    [HideInInspector]
    public List<HybridIKJoint> nodes;
    private bool isInitialized = false;

    [Header("Joint Limits")]
    public bool enableJointLimits = true;
    public float jointLimitStrength = 0.75f;
    public bool useStrictLimits = false;

    [Header("Stretch limits")]
    public bool enableStretch = true;

    [Header("Angular Motion Constraints (degrees)")]
    public bool useAngularMotionMotorConstraints = false;
    public float jointAngularAcceleration = 20f;
    public float jointMaxAnglarVelocity = 45f;
    [Range(0, 1)]
    public float jointAngularSmoothingFactor = 0.9f;

    [Header("Stretch Motion Constraints (m)")]
    public bool useStretchMotionMotorConstraints = false;
    public float jointStretchAcceleration = 1f;
    public float jointMaxStretchVelocity = 1f;
    [Range(0, 1)]
    public float jointStretchSmoothingFactor = 0.9f;

    [Header("Keyframe Constraints")]
    public bool enableKeyframeConstraints = true;
    public bool strictlyProjectToKeyframeSpace = false;
    private bool hasKeyframeConstraints = false;

    [HideInInspector]
    public List<HybridIKConstraint> constraints;

    public HybridIKJoint GetEndIKNode()
    {
        if (nodes == null || nodes.Count == 0)
            return null;
        return nodes[nodes.Count - 1];
    }

    public HybridIKJoint GetRootIKNode()
    {
        if (nodes == null || nodes.Count == 0)
            return null;
        return nodes[0];
    }

    #region DebugDraw
    void OnDrawGizmos()
    {
        if (nodes == null || nodes.Count == 0) return;

        foreach (HybridIKConstraint c in constraints)
        {
            if (c.positionKeys.Count > 0)
            {

                for (int i = 0; i < c.positionKeys.Count; ++i)
                {
                    ConstraintPositionKey k = c.positionKeys[i];

                    if (i % 4 == 0) Gizmos.color = Color.green;
                    if (i % 4 == 1) Gizmos.color = Color.blue;
                    if (i % 4 == 2) Gizmos.color = Color.yellow;
                    if (i % 4 == 3) Gizmos.color = Color.magenta;

                    HybridIKJoint root = GetRootIKNode();
                    HybridIKJoint endIKNode = GetEndIKNode();
                    Transform targetSpace = root.jointTransform.parent;
                    Transform parentTargetSpace = root.jointTransform.parent;

                    // Draw Keyed Skeleton Pose
                    foreach (HybridIKJoint node in nodes)
                    {
                        if (node.parentIndex > -1 && i < node.keyedRotations.Count)
                        {
                            targetSpace = node.targetIKSpace != null ? node.targetIKSpace : root.jointTransform.parent;
                            parentTargetSpace = nodes[node.parentIndex].targetIKSpace != null ? nodes[node.parentIndex].targetIKSpace : root.jointTransform.parent;
                            //parentTargetSpace = targetSpace = root.jointTransform.parent;

                            Gizmos.DrawLine(targetSpace != null ? targetSpace.TransformPoint(node.keyedPositions[i]) : node.keyedPositions[i],
                                parentTargetSpace != null ? parentTargetSpace.TransformPoint(nodes[node.parentIndex].keyedPositions[i]) : nodes[node.parentIndex].keyedPositions[i]);
                        }
                    }

                    // Draw keyed end target
                    Gizmos.DrawWireSphere(k.GetEndTargetPosition(endIKNode.targetIKSpace != null ? endIKNode.targetIKSpace : root.jointTransform.parent), endIKNode.jointRadius);
                }

            }

            Gizmos.color = Color.white;
        }

        if (constraintsIKHullGenerated == true && IKHullTris != null && IKHullTris.Count > 0)
        {
            for (int i = 0; i < IKHullTris.Count; i += 3)
            {
                Vector3 v1 = IKHullVerts[IKHullTris[i]];
                Vector3 v2 = IKHullVerts[IKHullTris[i + 1]];
                Vector3 v3 = IKHullVerts[IKHullTris[i + 2]];
            }
        }
    }

    public void DrawDebugSkeleton()
    {
        foreach (HybridIKJoint node in nodes)
        {
            if (node.parent != null)
                Debug.DrawLine(node.targetPosition, node.parent.targetPosition);
        }
    }

    #endregion

    #region Initialization & Chain processing
    public void ResetAll()
    {
        if (nodes != null)
        {
            nodes.Clear();
            nodes = null;
        }
    }

    public void ReprocessJoints()
    {
        HybridIKJoint endIKNode = GetEndIKNode();
        HybridIKJoint rootIKNode = GetRootIKNode();
        Transform lastIKSpace = rootIKNode.jointTransform.parent;
        Transform endIKSpace = rootIKNode.jointTransform.parent;

        // find endNodeSpace
        foreach (HybridIKJoint n in nodes)
        {
            if (n != endIKNode && !n.enableKeyframeConstraints)
                endIKSpace = n.jointTransform;

            n.cachedRotation = n.jointTransform.localRotation;
            n.cachedPosition = n.jointTransform.localPosition;
            n.jointTransform.localRotation = n.initialRotation;
            n.jointTransform.localPosition = n.initialPosition;
        }

        //already pre-processed - so cache child/parent nodes and return
        foreach (HybridIKJoint n in nodes)
        {
            if (n.parentIndex > -1)
                n.parent = nodes[n.parentIndex];
            if (n.childIndex > -1)
                n.child = nodes[n.childIndex];
            n.jointLimit = n.jointTransform.GetComponent<DynamicJointLimit>();

            // Re-cache constraints
            if (!n.overrideConstraint && constraints != null && constraints.Count > 0)
            {
                HybridIKConstraint constraint = constraints.Find(c => c.jointTransform == n.jointTransform);
                constraint.constrainRotation = true;
                if (constraint != null)
                    n.constraint = constraint;

                // change keyframe relative to target
                for (int i = 0; i < n.constraint.positionKeys.Count; ++i)
                {
                    ConstraintPositionKey k = n.constraint.positionKeys[i];

                    // change keyedPos frame
                    Vector3 wPos = n.targetIKSpace == null ? rootIKNode.jointTransform.parent.TransformPoint(n.keyedPositions[i])
                        : n.targetIKSpace.TransformPoint(n.keyedPositions[i]);
                    n.keyedPositions[i] = lastIKSpace.InverseTransformPoint(wPos);
                    wPos = k.GetEndTargetPosition(k.targetSpace ?? rootIKNode.jointTransform.parent);
                    k.SetEndTargetPosition(wPos, endIKSpace);
                }

                n.targetIKSpace = lastIKSpace;
                if (!n.enableKeyframeConstraints)
                    lastIKSpace = n.jointTransform;

                if (n == endIKNode)
                {
                    n.enableKeyframeConstraints = false;
                    n.overrideConstraint = false;
                }
            }

            if (n.overrideConstraint && n.constraint != null && n.constraint.positionKeys != null)
            {
                n.constraint.positionKeys.Clear();
            }

            n.jointLimit = n.jointTransform.GetComponent<DynamicJointLimit>();
        }

        foreach (HybridIKJoint n in nodes)
        {
            n.jointTransform.localRotation = n.cachedRotation;
            n.jointTransform.localPosition = n.cachedPosition;
        }
    }

    public void ProcessChain()
    {
        if (endNode == null) return;

        if (nodes != null && nodes.Count > 0)
        {
            ReprocessJoints();
            return;
        }

        constraints = new List<HybridIKConstraint>();

        HybridIKJoint child = null;
        nodes = new List<HybridIKJoint>();
        Transform node = endNode;

        while (node != null)
        {
            HybridIKJoint ikNode = new HybridIKJoint(node, child, nodeRadius);

            child = ikNode;

            //ikNode.constraint = null;
            if (constraints != null && constraints.Count > 0)
            {
                HybridIKConstraint constraint = constraints.Find(c => c.jointTransform == ikNode.jointTransform);
                if (constraint != null)
                    ikNode.constraint = constraint;
            }

            nodes.Add(ikNode);
            if (node == rootNode) break;

            node = node.parent;
            while (node.tag.Contains("ExcludeFromIK"))
            {
                node = node.parent;
                if (node == null || node == rootNode)
                    break;
            }
        }

        nodes.Reverse();

        for (int i = 0; i < nodes.Count; ++i)
        {
            HybridIKJoint n = nodes[i];

            n.zeroRotation = n.jointTransform.localRotation;
            n.zeroPosition = n.jointTransform.localPosition;

            if (i < nodes.Count - 1)
            {
                n.child = nodes[i + 1];
                n.childIndex = i + 1;
                n.IsEndNode = false;
            }
            else
            {
                n.child = null;
                n.childIndex = -1;
                n.IsEndNode = true;
            }
            if (i > 0)
            {
                n.parent = nodes[i - 1];
                n.parentIndex = i - 1;
                n.boneLength = (n.jointTransform.position - nodes[i - 1].jointTransform.position).magnitude;
            }
            else
            {
                n.parent = null;
                n.parentIndex = -1;
            }
        }


        isInitialized = true;
    }

    public void ResetToZeroPose()
    {
        if (nodes == null || nodes.Count == 0) return;

        foreach (HybridIKJoint node in nodes)
        {
            node.jointTransform.localRotation = node.zeroRotation;
            node.jointTransform.localPosition = node.zeroPosition;
        }
    }

    public void ResetToLastFramePose()
    {
        if (nodes == null || nodes.Count == 0) return;

        foreach (HybridIKJoint node in nodes)
        {
            node.jointTransform.localRotation = node.lastFrameRotation;
            node.jointTransform.localPosition = node.lastFramePosition;
        }
    }

    public void ReinitializeStretch()
    {
        foreach (HybridIKJoint node in nodes)
        {
            node.jointTransform.localPosition = node.initialPosition;
        }
    }

    public bool IsInitialized()
    {
        return nodes != null && nodes.Count > 0;
    }
    #endregion

    #region Limits & Constraints
    public bool UseKeyframeConstraints { get { return enableKeyframeConstraints && hasKeyframeConstraints; } }

    public bool HasStretchEnabled(HybridIKJoint node)
    {
        return enableStretch && node.enableStretch;
    }

    public bool HasPositionConstraint(HybridIKJoint node)
    {
        return (UseKeyframeConstraints || node.overrideConstraint) && node.HasPositionConstraint();
    }

    public bool HasRotationConstraint(HybridIKJoint node)
    {
        return (UseKeyframeConstraints || node.overrideConstraint) && node.HasRotationConstraint();
    }

    public bool HasIKConstraint(HybridIKJoint node)
    {
        return (UseKeyframeConstraints || node.overrideConstraint) && node.HasIKConstraint();
    }

    public void ApplyJointLimits(HybridIKJoint parentNode, float jointLimitStrength = 1)
    {
        if (parentNode.jointLimit == null) return;
        parentNode.jointLimit.Apply(jointLimitStrength);
    }

    #endregion

    #region FABRIK

    public void ResolveFABRIKStretch(HybridIKJoint targetJoint, HybridIKJoint relativeJoint, float baseBonelength, float minBoneLength, float maxBoneLength)
    {
        Vector3 toRelative = targetJoint.targetPosition - relativeJoint.targetPosition;
        if (toRelative.magnitude > maxBoneLength)
        {
            targetJoint.targetPosition = relativeJoint.targetPosition + toRelative.normalized * maxBoneLength;
        }
        else if (toRelative.magnitude < minBoneLength)
        {
            targetJoint.targetPosition = relativeJoint.targetPosition + toRelative.normalized * minBoneLength;
        }

    }

    private void FABRIK_BackwardStep(bool useConstraints = false, bool allowStretch = true)
    {
        HybridIKJoint rootIKNode = GetRootIKNode();
        HybridIKJoint endIKNode = GetEndIKNode();
        HybridIKJoint node = endIKNode;
        endIKNode.targetPosition = targetPosition;
        while (node.parent != null)
        {
            HybridIKJoint parent = node.parent;
            if (parent == rootIKNode) break;

            if (useConstraints)
            {
                if (HasPositionConstraint(parent))
                {
                    parent.targetPosition = parent.constraint.GetPosition();

                    node = parent;
                    continue;
                }
            }

            float minBoneLength = allowStretch ? node.boneLength - node.inwardsStretch : node.boneLength;
            float maxBoneLength = allowStretch ? node.boneLength + node.outwardsStretch : node.boneLength;
            minBoneLength = maxBoneLength = node.boneLength;
            ResolveFABRIKStretch(parent, node, node.boneLength, minBoneLength, maxBoneLength);

            node = parent;
            if (node == rootIKNode) break;
        }
    }

    private void FABRIK_ForwardStep(bool useConstraints = false, bool allowStretch = true)
    {
        HybridIKJoint rootIKNode = GetRootIKNode();
        HybridIKJoint endIKNode = GetEndIKNode();
        HybridIKJoint node = rootIKNode;

        while (node.child != null)
        {
            HybridIKJoint child = node.child;
            if (child == endIKNode) break;

            if (useConstraints)
            {
                if (HasPositionConstraint(child))
                {
                    child.targetPosition = child.constraint.GetPosition();
                    node = child;
                    continue;
                }
            }

            float minBoneLength = allowStretch ? child.boneLength - child.inwardsStretch : child.boneLength;
            float maxBoneLength = allowStretch ? child.boneLength + child.outwardsStretch : child.boneLength;

            ResolveFABRIKStretch(child, node, node.boneLength, minBoneLength, maxBoneLength);

            node = child;
            if (node == endIKNode) break;
        }
    }

    public void FABRIK_Step(int numIterations = 2, bool useConstraints = false, float jointLimitStrength = 0)
    {
        if (mode == IKMode.CCD) return;
        for (int i = 0; i < numIterations; ++i)
        {
            FABRIK_BackwardStep(useConstraints);
            FABRIK_ForwardStep(useConstraints);
        }
        AlignTransformsToTargets(jointLimitStrength);
    }

    #endregion

    #region CCD
    public void CCD_ForwardStep(bool useConstraints)
    {
        HybridIKJoint rootIKNode = nodes[0];
        HybridIKJoint endIKNode = nodes[nodes.Count - 1];

        endIKNode.isPositionConstrained = true;
        endIKNode.targetPosition = targetTransform.position;

        List<HybridIKJoint> ikTargetNodes = nodes.FindAll(n => (useConstraints && HasPositionConstraint(n)) || n.IsEndNode);

        foreach (HybridIKJoint targetIKNode in ikTargetNodes)
        {
            Vector3 targetPosition = targetTransform.position;
            Vector3 targetLocalPosition = targetIKNode.targetLocalPosition;
            if (HasPositionConstraint(targetIKNode))
            {
                targetPosition = targetIKNode.constraint.GetPosition();
                targetLocalPosition = targetIKNode.constraint.GetLocalPosition();
            }
            bool pastTarget = false;

            for (int i = 0; i < nodes.Count; i++)
            {
                bool solvingEndNode = targetIKNode.IsEndNode || pastTarget;
                if (!useConstraints) solvingEndNode = true;
                Vector3 originalPosition = solvingEndNode ? endIKNode.GetCurrentPositionWorld() : targetIKNode.GetCurrentPositionWorld();
                HybridIKJoint node = nodes[i];

                if (node.parent != null)
                {
                    SwingAlignToParticle(node.parent, originalPosition, targetPosition, jointLimitStrength, true, solvingEndNode);
                }

                if (!targetIKNode.IsEndNode && node == targetIKNode)
                {
                    targetPosition = targetTransform.position;
                    pastTarget = true;
                }
            }
        }
    }

    public void CCD_BackwardStep(bool useConstraints)
    {
        HybridIKJoint rootIKNode = nodes[0];
        HybridIKJoint endIKNode = nodes[nodes.Count - 1];

        endIKNode.isPositionConstrained = true;
        endIKNode.targetPosition = targetTransform.position;

        List<HybridIKJoint> ikTargetNodes = nodes.FindAll(n => (useConstraints && HasPositionConstraint(n)) || n.IsEndNode);

        foreach (HybridIKJoint targetIKNode in ikTargetNodes)
        {
            Vector3 targetPosition = targetTransform.position;
            Vector3 targetLocalPosition = targetIKNode.targetLocalPosition;

            if (HasPositionConstraint(targetIKNode))
            {
                targetPosition = targetIKNode.constraint.GetPosition();
                targetLocalPosition = targetIKNode.constraint.GetLocalPosition();
            }

            bool pastTarget = false;

            targetPosition = targetTransform.position;
            for (int i = nodes.Count - 1; i > 0; i--)
            {
                bool solvingEndNode = !(!targetIKNode.IsEndNode && pastTarget);

                solvingEndNode &= targetIKNode.parent == null || !HasIKConstraint(targetIKNode.parent);
                if (!useConstraints) solvingEndNode = true;

                Vector3 originalPosition = !targetIKNode.IsEndNode && pastTarget ? targetIKNode.GetCurrentPositionWorld() : endIKNode.GetCurrentPositionWorld();
                HybridIKJoint node = nodes[i];

                if (node.parent != null)
                {
                    SwingAlignToParticle(node.parent, originalPosition, targetPosition, jointLimitStrength, true, solvingEndNode);
                }

                if (!targetIKNode.IsEndNode && node == targetIKNode)
                {
                    pastTarget = true;
                    targetPosition = targetIKNode.constraint.GetPosition();
                }
            }

        }
    }

    public void CCD_Step(int numIterations = 2, bool useConstraints = false)
    {
        if (mode == IKMode.FABRIK) return;

        for (int i = 0; i < numIterations; ++i)
        {
            CCD_ForwardStep(useConstraints);
            CCD_BackwardStep(useConstraints);
        }
    }
    #endregion

    #region Stretch

    public void StretchAlignToParticle(HybridIKJoint parentNode, Vector3 originalPos, Vector3 targetPos)
    {
        if (!enableStretch) return;

        if (!parentNode.enableStretch || parentNode.stretchLimits == null) return;

        Vector3 delta = targetPos - originalPos;

        if (parentNode.stretchLimits.targetSpace == null)
            parentNode.stretchLimits.targetSpace = parentNode.jointTransform.transform;

        Vector3 projected = parentNode.stretchLimits.ApplyStretchLimit(delta, parentNode.initialPosition);
        //if ((targetPos - projected).magnitude < (targetPos - originalPos).magnitude)
        parentNode.jointTransform.position += projected;
    }

    public void StretchStep()
    {
        if (!enableStretch) return;

        HybridIKJoint endJoint = GetEndIKNode();
        Vector3 targetPos = targetTransform.position;
        foreach (HybridIKJoint j in nodes)
        {
            if (j == endJoint) return;
            Vector3 endPos = endJoint.GetCurrentPositionWorld();
            StretchAlignToParticle(j, endPos, targetPos);
        }
    }
    #endregion


    #region IK Alignment & Rotation
    public Quaternion GetSwingRotation(Transform parentTransform, Vector3 originalPos, Vector3 targetPos)
    {
        // SWING ROTATION
        Vector3 v1 = originalPos - parentTransform.position; v1.Normalize();
        Vector3 v2 = targetPos - parentTransform.position; v2.Normalize();

        v1 = parentTransform.InverseTransformVector(v1);
        v2 = parentTransform.transform.InverseTransformVector(v2);

        return Quaternion.FromToRotation(v1.normalized, v2.normalized);
    }

    public void SwingAlignToParticle(HybridIKJoint parentNode, Vector3 originalPos, Vector3 targetPos, float jointLimitStrength = 1, bool IKMode = false, bool solvingForEndNode = false)
    {
        Transform parentTransform = parentNode.jointTransform;
        Quaternion swingTargetRotation = GetSwingRotation(parentTransform, originalPos, targetPos);

        Quaternion originalRot = parentTransform.localRotation;
        Quaternion fullRotation = parentTransform.localRotation * swingTargetRotation;
        parentTransform.localRotation = fullRotation;

        if (IKMode)
        {
            if (constrainEndOrientation && parentNode.child.IsEndNode)
            {
                if (constrainInGlobalSpace)
                {
                    parentNode.jointTransform.rotation = targetTransform.rotation;
                }
                else
                {
                    parentNode.jointTransform.localRotation = targetTransform.rotation;
                }
            }

            //if (!solvingForEndNode && HasRotationConstraint(parentNode))
            if (HasRotationConstraint(parentNode))
            {
                    parentNode.jointTransform.localRotation = parentNode.constraint.GetRotation();
            }
        }

        if (enableJointLimits && jointLimitStrength > 0)
            ApplyJointLimits(parentNode, jointLimitStrength);
    }

    public void AlignTransformsToTargets(float jointLimitStrength = 1)
    {
        HybridIKJoint rootIKNode = GetRootIKNode();
        HybridIKJoint endIKNode = GetEndIKNode();
        rootIKNode.jointTransform.position = rootIKNode.targetPosition;

        for (int i = 0; i < nodes.Count; i++)
        {
            HybridIKJoint node = nodes[i];
            if (node.parent != null)
            {
                SwingAlignToParticle(node.parent, node.GetCurrentPositionWorld(), node.targetPosition, jointLimitStrength);
            }
        }
    }

    private void SnapParticlesToInitialJointPositions()
    {
        foreach (HybridIKJoint n in nodes)
        {
            n.targetPosition = n.initialPositionWorld;
        }
    }

    private void SnapParticlesToActualJointPositions()
    {
        foreach (HybridIKJoint n in nodes)
        {
            Vector3 cachedPos = n.jointTransform.position;
            if (n.jointTransform.parent == null || n.jointTransform.parent.parent == null)
                n.jointTransform.localPosition = n.initialPosition;
            n.targetPosition = n.jointTransform.position;
            n.jointTransform.position = cachedPos;
        }
    }

    #endregion

    #region IK Keyframe Constraints

    public void PoseToKeyframe(int keyframeID)
    {
        foreach (HybridIKJoint node in nodes)
        {
            node.jointTransform.localRotation = node.keyedRotations[keyframeID];
            node.jointTransform.localPosition = node.keyedLocalPositions[keyframeID];
        }
    }

    public Vector3 ProjectEndTargetToConstraintsHull(Vector3 targetPoint)
    {
        if (IKHullVerts == null)
            IKHullVerts = new List<Vector3>();

        //update constraints hull
        HybridIKJoint rootIKNode = GetRootIKNode();
        HybridIKJoint endIKNode = GetEndIKNode();
        if (endIKNode.constraint != null)
        {
            List<ConstraintPositionKey> targetKeys = endIKNode.constraint.positionKeys;
            if (targetKeys != null && targetKeys.Count > 0)
            {
                for (int i = 0; i < targetKeys.Count; ++i)
                {
                    ConstraintPositionKey k = endIKNode.constraint.positionKeys[i];
                    if (IKHullVerts.Count < i + 1) IKHullVerts.Add(Vector3.zero);
                    IKHullVerts[i] = k.GetEndTargetPosition(endIKNode.targetIKSpace != null ? endIKNode.targetIKSpace : rootIKNode.jointTransform.parent);
                }
            }
        }

        if (IKHullVerts == null || IKHullVerts.Count == 0)
        {
            return targetPoint;
        }
        if (IKHullVerts.Count == 1)
        {
            return targetPoint;
        }
        if (IKHullVerts.Count == 2)
        {
            return (GeometryMathHelperUtils.ProjectPointOnLineSegment(IKHullVerts[0], IKHullVerts[1], targetPoint));
        }
        if (IKHullVerts.Count == 3)
        {
            Vector3 projectPoint = GeometryMathHelperUtils.ProjectPointOnTriangle(IKHullVerts[0], IKHullVerts[1], IKHullVerts[2], targetPoint);
            return projectPoint;
        }

        if (IKHullVerts.Count > 3)
        {
            return targetPoint;
        }

        return targetPoint;
    }

    public void UpdateKeyframeConstraintKeys()
    {
        HybridIKJoint rootIKNode = GetRootIKNode();
        HybridIKJoint endIKNode = GetEndIKNode();

        float weightTotal = 0;
        int fullWeightIndex = -1;

        if (endIKNode.constraint == null) return;

        List<ConstraintPositionKey> targetKeys = endIKNode.constraint.positionKeys;

        hasKeyframeConstraints = targetKeys != null && targetKeys.Count > 0;
        if (!hasKeyframeConstraints)
            return;

        Vector3 currentEndTarget = ProjectEndTargetToConstraintsHull(targetTransform.position);

        if (targetKeys != null && targetKeys.Count > 0)
        {
            // find weights
            for (int i = 0; i < targetKeys.Count; ++i)
            {
                ConstraintPositionKey k = endIKNode.constraint.positionKeys[i];
                Vector3 keyedEndTarget = k.GetEndTargetPosition(endIKNode.targetIKSpace != null ? endIKNode.targetIKSpace : rootIKNode.jointTransform.parent);

                float distance = Vector3.Distance(currentEndTarget, keyedEndTarget);

                float angle = Vector3.Angle(currentEndTarget - rootIKNode.GetCurrentPositionWorld(), keyedEndTarget - rootIKNode.GetCurrentPositionWorld());

                if (distance <= Mathf.Epsilon || angle <= Mathf.Epsilon)
                {
                    fullWeightIndex = i;
                }

                float weight = (1f / (distance + Mathf.Epsilon) * (1 / (angle + Mathf.Epsilon)));

                weightTotal += weight;
                targetKeys[i].cachedWeight = weight;
            }
        }


        foreach (HybridIKJoint node in nodes)
        {
            if (node.enableKeyframeConstraints && !node.overrideConstraint)
            {
                node.cachedRotation = node.jointTransform.localRotation;
                node.cachedPosition = node.jointTransform.localPosition;
                node.jointTransform.localRotation = Quaternion.identity;
                //node.targetLocalPosition = node.initialPosition;
                node.jointTransform.localPosition = Vector3.zero;
                node.targetRotation = Quaternion.identity;
            }
        }

        // to avoid divide by zero errors
        if (fullWeightIndex >= 0)
        {
            weightTotal = 1;
            for (int i = 0; i < targetKeys.Count; ++i)
            {
                targetKeys[i].cachedWeight = fullWeightIndex == i ? 1 : 0;
            }
        }

        if (targetKeys != null)
        {
            for (int i = 0; i < targetKeys.Count; ++i)
            {
                float w = weightTotal < Mathf.Epsilon || targetKeys.Count == 1 ? 1 : targetKeys[i].cachedWeight / weightTotal;
                foreach (HybridIKJoint node in nodes)
                {
                    if (node.enableKeyframeConstraints && !node.overrideConstraint)
                    {
                        if (w > 0)
                        {
                            Quaternion q = Quaternion.Slerp(Quaternion.identity, node.keyedRotations[i], w);
                            if (!float.IsNaN(q.w))
                                node.jointTransform.localRotation *= q;

                            if (!float.IsNaN(node.keyedLocalPositions[i].x))
                                node.jointTransform.localPosition += node.keyedLocalPositions[i] * w;
                        }

                    }
                }
            }
        }

        foreach (HybridIKJoint node in nodes)
        {
            bool hasKeyframeConstraint = node.enableKeyframeConstraints && !node.overrideConstraint;
            if (node.constraint.targetTransform != null || node.constraint.positionKeys == null || node.constraint.positionKeys.Count == 0)
            {
                if (hasKeyframeConstraint)
                {
                    node.constraint.targetPosition = node.constraint.GetPosition();
                    node.constraint.targetRotation = node.constraint.GetRotation();
                    node.constraint.targetLocalPosition = node.constraint.GetLocalPosition();
                }
            }
            else
            {
                node.constraint.targetLocalPosition = node.jointTransform.localPosition;
                node.constraint.targetPosition = node.jointTransform.position;
                node.constraint.targetRotation = node.jointTransform.localRotation;
                if (node.constraint.targetTransform != null)
                {
                    node.constraint.targetTransform.position = node.jointTransform.position;
                    node.constraint.targetTransform.rotation = node.jointTransform.localRotation;
                }

            }
        }
    }

    List<Vector3> IKHullVerts = null;
    List<Vector3> IKHullNormals = null;
    List<int> IKHullTris = null;
    bool constraintsIKHullGenerated = false;

    #endregion

    void Start()
    {
        ProcessChain();

        foreach (HybridIKJoint n in nodes)
        {
            n.lastFrameRotation = n.jointTransform.localRotation;
            n.lastFramePosition = n.jointTransform.localPosition;
        }
    }

    private void ReinitializeTransforms()
    {
        switch (initializePoseFrom)
        {
            case InitializePoseFrom.INITIAL_POSE:
                ResetToZeroPose();
                break;
            case InitializePoseFrom.LAST_FRAME:
                ResetToLastFramePose();
                break;
        }

        foreach (HybridIKJoint node in nodes)
        {
            node.cachedInitialRotation = node.jointTransform.localRotation;
            node.cachedLocalPosition = node.jointTransform.localPosition;
        }
    }

    public void ApplyAngularMotionConstraints()
    {
        float largestAngle = 0;
        foreach (HybridIKJoint node in nodes)
        {
            float a = Quaternion.Angle(node.lastFrameRotation, node.jointTransform.localRotation);
            largestAngle = Mathf.Max(a, largestAngle);
        }

        foreach (HybridIKJoint node in nodes)
        {
            float angle = Quaternion.Angle(node.lastFrameRotation, node.jointTransform.localRotation);
            angle *= (angle / largestAngle);

            Quaternion targetRotation = node.jointTransform.localRotation;
            float t = Time.deltaTime;
            float targetSpeed = angle / t;
            if (targetSpeed > this.jointMaxAnglarVelocity)
            {
                targetSpeed = this.jointMaxAnglarVelocity;
            }

            //check deceleration
            float timeToDecelerate = node.currentAngularVelocity / this.jointAngularAcceleration;
            float angleToDecelerate = timeToDecelerate * node.currentAngularVelocity * 0.5f;
            bool shouldDescelerate = angleToDecelerate >= angle;// Mathf.Abs(angleToDecelerate) <= Mathf.Abs(angle);

            if (shouldDescelerate)
            {
                targetSpeed = 0;
            }

            float delta = targetSpeed - node.currentAngularVelocity;
            float sign = Mathf.Sign(delta);
            float deltaVelocity = Time.deltaTime * jointAngularAcceleration * sign;
            if (Mathf.Abs(deltaVelocity) > Mathf.Abs(delta))
                node.currentAngularVelocity = targetSpeed;
            else
                node.currentAngularVelocity += deltaVelocity;

            float fAngle = node.currentAngularVelocity * t;// * Mathf.Sign(targetSpeed);
            if (fAngle > angle) fAngle = angle;

            node.jointTransform.localRotation = node.lastFrameRotation;
            //node.jointTransform.localRotation = Quaternion.Slerp(node.lastFrameRotation, targetRotation, 1f);
            Quaternion target = Quaternion.RotateTowards(node.lastFrameRotation, targetRotation, fAngle);
            if (Quaternion.Angle(node.jointTransform.localRotation, targetRotation) < 1f)
            {
                //node.jointTransform.localRotation = targetRotation;
            }
            else
            {
                node.jointTransform.localRotation = Quaternion.Slerp(node.lastFrameRotation, target, (1 - jointAngularSmoothingFactor) + (Time.deltaTime * angle * 5f));
            }
        }
    }

    public void ApplyStretchMotionConstraints()
    {
        float largestDistance = 0;
        foreach (HybridIKJoint node in nodes)
        {
            float a = Vector3.Distance(node.lastFramePosition, node.jointTransform.localPosition);
            largestDistance = Mathf.Max(a, largestDistance);
        }

        foreach (HybridIKJoint node in nodes)
        {
            float distance = Vector3.Distance(node.lastFramePosition, node.jointTransform.localPosition);
            distance *= (distance / largestDistance);

            Vector3 targetPosition = node.jointTransform.localPosition;
            float t = Time.deltaTime;
            float targetSpeed = distance / t;
            if (targetSpeed > this.jointMaxStretchVelocity)
            {
                targetSpeed = this.jointMaxStretchVelocity;
            }

            //check deceleration
            float timeToDecelerate = node.currentStretchVelocity / this.jointStretchAcceleration;
            float distToDecelerate = timeToDecelerate * node.currentStretchVelocity * 0.5f;
            bool shouldDescelerate = distToDecelerate >= distance;// Mathf.Abs(angleToDecelerate) <= Mathf.Abs(angle);

            if (shouldDescelerate)
            {
                targetSpeed = 0;
            }

            float delta = targetSpeed - node.currentStretchVelocity;
            float sign = Mathf.Sign(delta);
            float deltaVelocity = Time.deltaTime * jointStretchAcceleration * sign;
            if (Mathf.Abs(deltaVelocity) > Mathf.Abs(delta))
                node.currentStretchVelocity = targetSpeed;
            else
                node.currentStretchVelocity += deltaVelocity;

            float fDist = node.currentStretchVelocity * t;// * Mathf.Sign(targetSpeed);
            if (fDist > distance) fDist = distance;

            node.jointTransform.localPosition = node.lastFramePosition;
            //node.jointTransform.localRotation = Quaternion.Slerp(node.lastFrameRotation, targetRotation, 1f);
            Vector3 target = Vector3.MoveTowards(node.lastFramePosition, targetPosition, fDist);
            if (Vector3.Distance(node.jointTransform.localPosition, targetPosition) < 1f)
            {
            }
            else
            {
                node.jointTransform.localPosition = Vector3.Slerp(node.lastFramePosition, target, (1 - jointStretchSmoothingFactor) + (Time.deltaTime * distance * 5f));
            }
        }
    }

    void LateUpdate()
    {
        if (!IsInitialized()) return;

        if (enableKeyframeConstraints)
            UpdateKeyframeConstraintKeys();

        targetPosition = targetTransform.position;

        HybridIKJoint rootIKNode = GetRootIKNode();
        HybridIKJoint endIKNode = GetEndIKNode();

        rootIKNode.targetPosition = rootIKNode.jointTransform.position;

        // ReInitialization
        ReinitializeTransforms();
        ReinitializeStretch();

        // IK Steps
        FABRIK_Step(numIterations, true, jointLimitStrength);
        CCD_Step(numIterations, true);        
        StretchStep();

        SnapParticlesToActualJointPositions();

        bool cachedEnableKeyframeConstraints = enableKeyframeConstraints;

        // Apply second pass with no constraints, focusing on end target only
        if (!enableKeyframeConstraints || !strictlyProjectToKeyframeSpace)
        {
            enableKeyframeConstraints = false;
            FABRIK_Step(numIterations, false, jointLimitStrength);
            CCD_Step(numIterations, false);
        }

        // With soft limits, apply a final pass to achieve target with joint limits disabled for the delta
        if (!useStrictLimits)
        {
            SnapParticlesToActualJointPositions();
            FABRIK_Step();
        }

        SnapParticlesToActualJointPositions();

        if (useAngularMotionMotorConstraints)
        {
            ApplyAngularMotionConstraints();
        }
        if (useStretchMotionMotorConstraints)
        {
            ApplyStretchMotionConstraints();
        }

        foreach (HybridIKJoint n in nodes)
        {
            n.lastFrameRotation = n.jointTransform.localRotation;
            n.lastFramePosition = n.jointTransform.localPosition;
        }

        enableKeyframeConstraints = cachedEnableKeyframeConstraints;
    }
}
