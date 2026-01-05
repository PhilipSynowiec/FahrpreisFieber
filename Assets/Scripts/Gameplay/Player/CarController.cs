using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float turnSpeedDeg = 140f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
    }

    private void FixedUpdate()
    {
        float forward = Input.GetAxisRaw("Vertical");   // W/S
        float turn = Input.GetAxisRaw("Horizontal");    // A/D

        // Rotate
        if (Mathf.Abs(turn) > 0.001f)
        {
            Quaternion rot = Quaternion.Euler(0f, turn * turnSpeedDeg * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * rot);
        }

        // Accelerate
        rb.AddForce(rb.rotation * Vector3.forward * (forward * acceleration), ForceMode.Acceleration);

        // Clamp planar speed
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        if (v.magnitude > maxSpeed) v = v.normalized * maxSpeed;
        rb.linearVelocity = new Vector3(v.x, 0f, v.z);
    }
}
