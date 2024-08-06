using UnityEngine;

public class VRobot : MonoBehaviour
{
    [SerializeField] private Transform head;

    private void Update()
    {
        if (head)
        {
            transform.position = head.position;
        }
    }
}
