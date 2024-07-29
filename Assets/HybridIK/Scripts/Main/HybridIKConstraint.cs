using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class HybridIKConstraint
{
    public Transform jointTransform;
    public Transform targetTransform;

    public Vector3 targetPosition;
    public Vector3 targetLocalPosition; //used for stretch constraints
    public Quaternion targetRotation = Quaternion.identity;

    public bool constrainPosition = true;
    public bool constrainRotation = false;

    public Vector3 GetPosition()
    {
        if (targetTransform != null)
            return targetTransform.position;
        return targetPosition;
    }

    public Vector3 GetLocalPosition()
    {
        //if (targetTransform != null)
        //    return targetTransform.position;
        return targetLocalPosition;
    }

    public Quaternion GetRotation()
    {
        if (targetTransform != null)
            return targetTransform.rotation;
        return targetRotation;
    }

    public List<ConstraintPositionKey> positionKeys;
}

[System.Serializable]
public class ConstraintPositionKey
{
    public Transform endTargetKeyTransform;
    public Transform constraintKeyTransform;

    [HideInInspector]
    public Vector3 endTargetPositionKey;
    [HideInInspector]
    public Vector3 constraintPositionValue;

    [HideInInspector]
    public Quaternion constraintRotationValue;
    [HideInInspector]
    public float cachedWeight = 0;
    [HideInInspector]
    public float cachedFade = 0;

    public Transform targetSpace = null;

    public Vector3 GetEndTargetPosition(Transform rootSpace = null)
    {
        return endTargetKeyTransform != null ? endTargetKeyTransform.position :
            (rootSpace != null ? rootSpace.TransformPoint(endTargetPositionKey) : endTargetPositionKey);
    }

    public void SetEndTargetPosition(Vector3 newPos, Transform rootSpace = null)
    {
        endTargetPositionKey = rootSpace != null ? rootSpace.InverseTransformPoint(newPos) : newPos;
        targetSpace = rootSpace;
    }

    public Vector3 GetConstraintKeyPosition()
    {
        return constraintKeyTransform != null ? constraintKeyTransform.position : constraintPositionValue;
    }

    public Quaternion GetConstraintKeyRotation()
    {
        return constraintKeyTransform != null ? constraintKeyTransform.rotation : constraintRotationValue;
    }

}
