using UnityEngine;

public class CarVisualMotion : MonoBehaviour
{
    [SerializeField] private Transform body;
    [SerializeField] private Transform[] wheels;
    [SerializeField] private float wheelRadius = 0.32f;
    private Rigidbody vehicle;
    private Quaternion restRotation;
    private Quaternion previousHeading;
    private float lastSpeed;
    private float roll, pitch;
    private void Awake()
    {
        vehicle = GetComponentInParent<Rigidbody>();
        restRotation = body.localRotation;
    }
    private void OnEnable()
    {
        if (body == null) return;
        if (vehicle != null) previousHeading = vehicle.rotation;
        lastSpeed = 0; roll = pitch = 0;
    }
    private void LateUpdate()
    {
        if (vehicle == null || body == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float speed = Vector3.Dot(vehicle.linearVelocity, vehicle.rotation * Vector3.forward);
        foreach (var wheel in wheels)
            if (wheel != null) wheel.Rotate(Vector3.right, speed / (wheelRadius * Mathf.Abs(transform.lossyScale.x)) * Mathf.Rad2Deg * dt, Space.Self);
        float yawRate = Mathf.DeltaAngle(previousHeading.eulerAngles.y, vehicle.rotation.eulerAngles.y) / dt;
        float blend = 1f - Mathf.Exp(-9f * dt);
        roll = Mathf.Lerp(roll, Mathf.Clamp(-yawRate * speed * .004f, -4f, 4f), blend);
        pitch = Mathf.Lerp(pitch, Mathf.Clamp((speed - lastSpeed) / dt * .10f, -2f, 2f), blend);
        body.localRotation = restRotation * Quaternion.Euler(pitch, 0, roll);
        previousHeading = vehicle.rotation; lastSpeed = speed;
    }
}
