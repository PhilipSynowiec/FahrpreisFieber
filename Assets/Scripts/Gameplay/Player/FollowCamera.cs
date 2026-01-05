using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 25f, 0f);
    [SerializeField] private bool lockTopDownRotation = true;

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;

        if (lockTopDownRotation)
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
