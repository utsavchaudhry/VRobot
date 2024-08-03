using UnityEngine;

public class VRobot : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Vector3 offset;

    private void Update()
    {
        FollowPlayer();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetHead();
        }
    }

    private void FollowPlayer()
    {
        if (!head)
        {
            return;
        }

        transform.position = head.position + offset;
    }

    public void ResetHead()
    {
        if (!head)
        {
            return;
        }

        transform.localEulerAngles = Vector3.up * head.localEulerAngles.y;
    }
}
