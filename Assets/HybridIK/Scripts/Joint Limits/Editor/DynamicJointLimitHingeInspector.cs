#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AshqarApps.DynamicJoint
{
    [CustomEditor(typeof(DynamicJointLimitHinge))]
    [CanEditMultipleObjects]
    public class DynamicJointLimitHingeInspector : Editor
    {
        private DynamicJointLimitHinge script { get { return target as DynamicJointLimitHinge; } }
        private Vector3 lastPoint, zeroPoint;

        float lastHingeOffset = 0;

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            base.OnInspectorGUI();
            //DrawDefaultInspector();

            int childCount = script.transform.childCount;
            GameObject childGO = null;
            for (int i = 0; i < script.transform.childCount; ++i)
            {
                if (!script.transform.GetChild(i).tag.Contains("ExcludeFromDynamics"))
                {
                    childGO = script.transform.GetChild(i).gameObject;
                    break;
                }
            }

            if (childGO != null)
            {
                if (GUILayout.Button("Align To Child Twist"))
                {
                    Vector3 toChild = childGO.transform.position - script.transform.position;
                    script.mainAxis = script.transform.InverseTransformDirection(toChild.normalized);
                    Vector3 secondaryAxis = new Vector3(script.mainAxis.y, script.mainAxis.z, script.mainAxis.x);
                    script.cross = Direction(Vector3.Cross(script.mainAxis, secondaryAxis));
                }
                else if (GUILayout.Button("Align To Child X"))
                {
                    Vector3 toChild = (childGO.transform.position - script.transform.position).normalized;
                    script.cross = script.transform.InverseTransformDirection(toChild);
                    //Vector3 secondaryAxis = new Vector3(script.cross.y, script.cross.z, script.cross.x);
                    Vector3 secondaryAxis = script.transform.InverseTransformDirection(script.transform.right);
                    Vector3.OrthoNormalize(ref secondaryAxis, ref script.cross);
                    //Vector3 cross = Direction(Vector3.Cross(script.cross, secondaryAxis));
                    script.mainAxis = secondaryAxis;
                }
                else if (GUILayout.Button("Align To Child Y"))
                {
                    Vector3 toChild = (childGO.transform.position - script.transform.position).normalized;
                    script.cross = script.transform.InverseTransformDirection(toChild);
                    Vector3 secondaryAxis = script.transform.InverseTransformDirection(script.transform.up);
                    Vector3.OrthoNormalize(ref secondaryAxis, ref script.cross);
                    script.mainAxis = secondaryAxis;
                }
                else if (GUILayout.Button("Align To Child Z"))
                {
                    Vector3 toChild = (childGO.transform.position - script.transform.position).normalized;
                    script.cross = script.transform.InverseTransformDirection(toChild);
                    Vector3 secondaryAxis = script.transform.InverseTransformDirection(script.transform.forward);
                    Vector3.OrthoNormalize(ref secondaryAxis, ref script.cross);
                    script.mainAxis = secondaryAxis;
                }
            }
            else
            {
                if (GUILayout.Button("Align To X"))
                {
                    Vector3 toChild = script.transform.right;
                    script.mainAxis = script.transform.InverseTransformDirection(toChild.normalized);
                    Vector3 secondaryAxis = new Vector3(script.mainAxis.y, script.mainAxis.z, script.mainAxis.x);
                    script.cross = Vector3.Cross(script.mainAxis, secondaryAxis);
                }

                if (GUILayout.Button("Align To Y"))
                {
                    Vector3 toChild = script.transform.up;
                    script.mainAxis = script.transform.InverseTransformDirection(toChild.normalized);
                    Vector3 secondaryAxis = new Vector3(script.mainAxis.y, script.mainAxis.z, script.mainAxis.x);
                    script.cross = Vector3.Cross(script.mainAxis, secondaryAxis);
                }

                if (GUILayout.Button("Align To Z"))
                {
                    Vector3 toChild = script.transform.forward;
                    script.mainAxis = script.transform.InverseTransformDirection(toChild.normalized);
                    Vector3 secondaryAxis = new Vector3(script.mainAxis.y, script.mainAxis.z, script.mainAxis.x);
                    script.cross = Vector3.Cross(script.mainAxis, secondaryAxis);
                }
            }

            if (GUI.changed) EditorUtility.SetDirty(script);

        }


        void UpdateZeroTransform()
        {
            if (lastHingeOffset != script.hingeAngleOffset)
            {
                lastHingeOffset = script.hingeAngleOffset;

            }
        }

        void OnSceneGUI()
        {
            if (!Application.isPlaying && !script.zeroRotationOverride) script.zeroRotation = script.transform.localRotation;
            if (script.mainAxis == Vector3.zero || script.cross == Vector3.zero) return;

            float minBoneLength = 0.15f;
            float boneLength = minBoneLength;
            if (script.transform.childCount > 0)
            {
                boneLength = Vector3.Distance(script.transform.position, script.transform.GetChild(0).position) / 4;
            }
            else
            {
                if (script.transform.parent != null)
                {
                    boneLength = Vector3.Distance(script.transform.position, script.transform.parent.position) / 4;
                }
            }

            boneLength = Mathf.Max(boneLength, minBoneLength);


            // Display the main axis
            DrawArrow(script.transform.position, Direction(script.mainAxis), colorDefault, "Axis", 0.02f);

            Handles.color = Color.white;
            GUI.color = Color.white;

            if (!Application.isPlaying)
            {

                // Selecting points
                Handles.color = colorHandles;
                Handles.Button(script.transform.position + Direction(script.mainAxis), script.transform.rotation, 0.02f, 0.02f, Handles.DotHandleCap);

                Vector3 selectedPoint = script.mainAxis;

                // Store point for undo
                Vector3 oldPoint = selectedPoint;

                // Manual input for the point position
                AddVector3(ref selectedPoint, "Point", script, GUILayout.Width(210));

                // Moving Points
                Vector3 pointWorld = Handles.PositionHandle(script.transform.position + Direction(selectedPoint), Quaternion.identity);
                Vector3 newPoint = InverseDirection(pointWorld - script.transform.position);
                if (newPoint != selectedPoint)
                {
                    if (!Application.isPlaying) Undo.RecordObject(script, "Move Limit Point");
                    script.mainAxis = newPoint.normalized;
                }
            }

            DrawHingeLimits(boneLength);

        }


        public void DrawHingeLimits(float boneLength)
        {
            Quaternion offsetRot = Quaternion.Euler(script.limitsOffset);

            Vector3 swing = Direction(offsetRot * script.mainAxis.normalized);

            //Vector3 secondaryAxis = new Vector3(script.mainAxis.y, script.mainAxis.z, script.mainAxis.x);
            //Vector3 cross = Direction(Vector3.Cross(script.mainAxis, secondaryAxis));
            if (script.cross.magnitude == 0) return;
            Vector3 cross = Direction(offsetRot * script.cross);
            Vector3.OrthoNormalize(ref swing, ref cross);
            Vector3 secondaryAxis = Vector3.Cross(script.mainAxis, cross);

            Handles.CircleHandleCap(0, script.transform.position, Quaternion.LookRotation(swing, cross), boneLength, EventType.Repaint);

            //DrawArrow(script.transform.position, cross * boneLength, colorDefault, " 0", 0.02f);
            DrawArrow(script.transform.position, cross * boneLength, colorDefault, " 0", 0.02f);

            Quaternion hingeOffset = Quaternion.AngleAxis(script.hingeAngleOffset, swing);

            // Arcs for the rotation limit
            Handles.color = colorDefaultTransparent;
            Handles.DrawSolidArc(script.transform.position, swing, hingeOffset * cross, -script.limitAngle, boneLength);
            Handles.DrawSolidArc(script.transform.position, swing, hingeOffset * cross, script.limitAngle, boneLength);

            Quaternion minRotation = Quaternion.AngleAxis(-script.limitAngle, swing);
            Vector3 minHandleDir = minRotation * (hingeOffset * cross);
            Handles.DrawLine(script.transform.position, script.transform.position + minHandleDir.normalized * boneLength);

            Quaternion maxRotation = Quaternion.AngleAxis(script.limitAngle, swing);
            Vector3 maxHandleDir = maxRotation * (hingeOffset * cross);
            Handles.DrawLine(script.transform.position, script.transform.position + maxHandleDir.normalized * boneLength);

            Handles.color = colorHandles;
            //Draw Editable Handles
            float originalLimit = script.limitAngle;
            float limitAngleMin = script.limitAngle;
            limitAngleMin = DrawLimitHandle(limitAngleMin, script.transform.position + (minHandleDir.normalized * boneLength * 1.25f), Quaternion.identity, 0.5f, "Limit", -10);
            if (limitAngleMin != script.limitAngle)
            {
                if (!Application.isPlaying) Undo.RecordObject(script, "Min Limit");
                script.limitAngle = limitAngleMin;
                script.hingeAngleOffset -= (limitAngleMin - originalLimit);
            }

            originalLimit = script.limitAngle;
            //Draw Editable Handles
            float limitAngleMax = script.limitAngle;
            limitAngleMax = DrawLimitHandle(limitAngleMax, script.transform.position + (maxHandleDir.normalized * boneLength * 1.25f), Quaternion.identity, 0.5f, "Limit", 10);
            if (limitAngleMax != script.limitAngle)
            {
                if (!Application.isPlaying) Undo.RecordObject(script, "Max Limit");
                script.limitAngle = limitAngleMax;
                script.hingeAngleOffset += (limitAngleMax - originalLimit);
            }

            // clamp limits
            script.limitAngle = Mathf.Clamp(script.limitAngle, 0, 180f);
            if (script.hingeAngleOffset < 0) script.hingeAngleOffset += 360;
            if (script.hingeAngleOffset > 360) script.hingeAngleOffset -= 360;

        }

        private Vector3 Direction(Vector3 v)
        {
            if (script.transform.parent == null) return script.zeroRotation * v;
            return script.transform.parent.rotation * (script.zeroRotation * v);
        }

        private Vector3 InverseDirection(Vector3 v)
        {
            if (script.transform.parent == null) return Quaternion.Inverse(script.zeroRotation) * v;

            return Quaternion.Inverse(script.zeroRotation) * Quaternion.Inverse(script.transform.parent.rotation) * v;
        }

        // Universal color pallettes
        public static Color colorDefault { get { return new Color(0.0f, 0.0f, 1.0f, 1.0f); } }

        public static Color colorDefaultTransparent
        {
            get
            {
                Color d = colorDefault;
                return new Color(d.r, d.g, d.b, 0.2f);
            }
        }

        public static Color colorHandles { get { return new Color(1.0f, 0.5f, 0.25f, 1.0f); } }
        public static Color colorRotationSphere { get { return new Color(1.0f, 1.0f, 1.0f, 0.1f); } }
        public static Color colorInvalid { get { return new Color(1.0f, 0.3f, 0.3f, 1.0f); } }
        public static Color colorValid { get { return new Color(0.2f, 1.0f, 0.2f, 1.0f); } }

        /*
         * Draws a custom arrow to the scene
         * */
        public static void DrawArrow(Vector3 position, Vector3 direction, Color color, string label = "", float size = 0.01f)
        {
            Handles.color = color;
            Handles.DrawLine(position, position + direction);
            Handles.SphereHandleCap(0, position + direction, Quaternion.identity, size, EventType.Repaint);
            Handles.color = Color.white;

            if (label != "")
            {
                GUI.color = color;
                Handles.Label(position + direction, label);
                GUI.color = Color.white;
            }
        }

        /*
         * Draws a handle for adjusting rotation limits in the scene
         * */
        public static float DrawLimitHandle(float limit, Vector3 position, Quaternion rotation, float radius, string label, float openingValue, bool reverseScaleDirection = false)
        {
            float scaleFactor = reverseScaleDirection ? -1 : 1;

            limit = scaleFactor * Handles.ScaleValueHandle(limit, position, rotation, radius, Handles.SphereHandleCap, 1);

            string labelInfo = label + ": " + limit.ToString();

            // If value is 0, draw a button to 'open' the value, because we cant scale 0
            if (limit == 0)
            {
                labelInfo = "Open " + label;

                if (Handles.Button(position, rotation, radius * 0.2f, radius * 0.07f, Handles.SphereHandleCap))
                {
                    limit = openingValue;
                }
            }

            Handles.Label(position, labelInfo);

            return limit;
        }


        public static void AddVector3(ref Vector3 input, string name, Object undoObject, params GUILayoutOption[] options)
        {
            Vector3 newValue = EditorGUILayout.Vector3Field(name, input, options);

            if (newValue != input && !Application.isPlaying)
            {
                Undo.RecordObject(undoObject, name);
            }

            input = newValue;
        }
    }
}
#endif

