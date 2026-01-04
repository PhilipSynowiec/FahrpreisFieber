using UnityEngine;

public class RotatePreview : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 60f;

    private void Update()
    {
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
    }
}
