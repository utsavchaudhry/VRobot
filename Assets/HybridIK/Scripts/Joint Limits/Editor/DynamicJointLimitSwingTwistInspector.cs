using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AshqarApps.DynamicJoint
{
    [CustomEditor(typeof(DynamicJointLimitSwingTwist))]
    [CanEditMultipleObjects]
    public class DynamicJointLimitSwingTwistInspector : Editor
    {
        private DynamicJointLimitSwingTwist script { get { return target as DynamicJointLimitSwingTwist; } }
        private int axisComponentSelected = 0;
        private Vector3 lastPoint, zeroPoint;
        private int selectedChild = 0;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            int childCount = script.transform.childCount;

            if (childCount == 1 && GUILayout.Button("Align To Child"))
            {
                Vector3 toChild = script.transform.GetChild(0).position - script.transform.position;
                script.mainAxis = script.transform.InverseTransformDirection(toChild.normalized);
                if (GUI.changed) EditorUtility.SetDirty(script);
            }
            else if (childCount > 1)
            {
                List<string> childGOs = new List<string>();

                //if (childGOs == null)
                {
                    for (int i = 0; i < script.transform.childCount; ++i)
                    {
                        childGOs.Add(script.transform.GetChild(i).name);
                    }
                }

                selectedChild = EditorGUILayout.Popup("Selected Child", selectedChild, childGOs.ToArray());

                //int selectedChild = 0;
                if (GUILayout.Button("Align To Selected Child"))
                {
                    GameObject childGO = script.transform.GetChild(selectedChild).gameObject;

                    if (childGO.transform.childCount > 0)
                    {
                        childGO = childGO.transform.GetChild(0).gameObject;

                        Vector3 toChild = childGO.transform.position - script.transform.position;
                        script.mainAxis = script.transform.InverseTransformDirection(toChild.normalized);
                        if (GUI.changed) EditorUtility.SetDirty(script);
                    }
                }
            }
        }

        void OnSceneGUI()
        {
            // Set defaultLocalRotation so that the initial local rotation will be the zero point for the rotation limit
            if (!Application.isPlaying && !script.zeroRotationOverride) script.zeroRotation = script.transform.localRotation;
            if (script.mainAxis == Vector3.zero) return;

            //DrawRotationSphere(script.transform.position);

            Handles.color = Color.white;
            GUI.color = Color.white;

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
            DrawArrow(script.transform.position, Direction(script.mainAxis).normalized * boneLength, colorDefault, "Axis", 0.02f);
            DrawArrow(script.transform.position, Direction(script.secondaryAxis) * boneLength, colorDefault, "Secondary Axis", 0.02f);

            if (!Application.isPlaying)
            {
                // Selecting points
                Handles.color = colorHandles;
                if (Handles.Button(script.transform.position + Direction(script.mainAxis.normalized) * boneLength * 1.25f, script.transform.rotation, 0.02f, 0.02f, Handles.DotHandleCap))
                {
                    axisComponentSelected = 0;
                }
                else if (Handles.Button(script.transform.position + Direction(script.secondaryAxis.normalized) * boneLength * 1.25f, script.transform.rotation, 0.02f, 0.02f, Handles.DotHandleCap))
                {
                    axisComponentSelected = 1;
                }

                Vector3 selectedPoint = axisComponentSelected == 0 ? script.mainAxis : script.secondaryAxis;

                // Store point for undo
                Vector3 oldPoint = selectedPoint;

                // Manual input for the point position
                AddVector3(ref selectedPoint, "Point", script, GUILayout.Width(210));
                //EditorGUILayout.Space();

                // Moving Points
                Vector3 pointWorld = Handles.PositionHandle(script.transform.position + (Direction(selectedPoint.normalized) * boneLength * 1.25f), Quaternion.identity);
                Vector3 newPoint = InverseDirection(pointWorld - script.transform.position);
                if (newPoint != selectedPoint)
                {
                    if (!Application.isPlaying) Undo.RecordObject(script, "Move Limit Point");
                    if (axisComponentSelected == 0)
                        script.mainAxis = newPoint;
                    else
                        script.secondaryAxis = newPoint;
                }
            }

            Vector3.OrthoNormalize(ref script.mainAxis, ref script.secondaryAxis);

            DrawSwingTwistLimits(boneLength);

        }


        public void DrawSwingTwistLimits(float boneLength)
        {
            Vector3 swing = script.mainAxis.normalized;
            Vector3 secondaryAxis = script.secondaryAxis.normalized; //new Vector3(swing.y, swing.z, swing.x);
            Vector3.OrthoNormalize(ref swing, ref secondaryAxis);
            Vector3 cross = Vector3.Cross(swing, secondaryAxis);

            // DRAW TWIST
            Quaternion offsetRot = Quaternion.Euler(script.limitsOffset);
            Handles.color = colorTwistLimit;
            Handles.DrawSolidArc(script.transform.position, Direction(offsetRot * swing), Direction(offsetRot * cross), script.twistLimit, boneLength);
            Handles.DrawSolidArc(script.transform.position, Direction(offsetRot * swing), Direction(offsetRot * cross), -script.twistLimit, boneLength);

            // Display limits
            lastPoint = script.transform.position;

            for (int i = 0; i < 360; i += 2)
            {
                //float evaluatedLimit = script.EvaluateSwingLimitAtOrthoAngle((float)(i));
                //float evaluatedLimit = script.curveX.Evaluate(Mathf.Abs(Mathf.Sin()));

                Quaternion offset = Quaternion.AngleAxis(i, swing);
                Quaternion evaluatedRotation = Quaternion.AngleAxis(script.limitAngle, offset * (cross));

                Color color = colorDefaultTransparent;

                Vector3 swingAxis = script.LimitSwing(evaluatedRotation, visualizeMode: true) * swing;
                swingAxis = offsetRot * swingAxis;

                Vector3 limitPoint = script.transform.position + Direction(swingAxis).normalized * boneLength;
                Handles.DrawLine(script.transform.position, script.transform.position + Direction(swingAxis).normalized * boneLength);

                Handles.color = color;
                if (i == 0) zeroPoint = limitPoint;

                if (i > 0)
                {
                    Handles.color = colorDefault;
                    Handles.DrawLine(limitPoint, lastPoint);
                    if (i == 358) Handles.DrawLine(limitPoint, zeroPoint);
                }

                lastPoint = limitPoint;
            }
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

        public static Color colorDefault { get { return new Color(0.0f, 1.0f, 1.0f, 0.5f); } }
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
        public static Color colorTwistLimit { get { return new Color(1.0f, 1.0f, 0.0f, 0.1f); } }
        public static Color colorInvalid { get { return new Color(1.0f, 0.3f, 0.3f, 1.0f); } }
        public static Color colorValid { get { return new Color(0.2f, 1.0f, 0.2f, 1.0f); } }

        /*
         * Draws the default rotation limit sphere to the scene
         * */
        public static void DrawRotationSphere(Vector3 position, float radius)
        {
            Handles.color = colorRotationSphere;
            Handles.SphereHandleCap(0, position, Quaternion.identity, radius, EventType.Repaint);
            Handles.color = Color.white;
        }

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
