using UnityEngine;

public class GizmoVisualizer : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        DrawSphere(transform);
    }

    private void DrawSphere(Transform sphereTransfrom, Transform lineStart = null)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(sphereTransfrom.position, 0.1f);

        if (lineStart)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(lineStart.position, sphereTransfrom.position);
        }

        if (sphereTransfrom.childCount > 0)
        {
            DrawSphere(sphereTransfrom.GetChild(0), sphereTransfrom);
        }
    }
}
