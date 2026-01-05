using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerInputSystem : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float accel = 18f;
    [SerializeField] private float decel = 22f;

    [Header("Steering")]
    [SerializeField] private float turnSpeedDeg = 180f;      // deg/sec at full steer
    [SerializeField] private float turnSpeedAtZero = 60f;    // deg/sec when standing

    private Rigidbody rb;

    private InputAction moveAction; // x=turn, y=forward
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        // Interp helps visuals
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");

        var wasd = moveAction.AddCompositeBinding("2DVector");
        wasd.With("Up", "<Keyboard>/w");
        wasd.With("Down", "<Keyboard>/s");
        wasd.With("Left", "<Keyboard>/a");
        wasd.With("Right", "<Keyboard>/d");

        var arrows = moveAction.AddCompositeBinding("2DVector");
        arrows.With("Up", "<Keyboard>/upArrow");
        arrows.With("Down", "<Keyboard>/downArrow");
        arrows.With("Left", "<Keyboard>/leftArrow");
        arrows.With("Right", "<Keyboard>/rightArrow");

        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    private void OnEnable() => moveAction.Enable();
    private void OnDisable() => moveAction.Disable();

    private void FixedUpdate()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        float steer = Mathf.Clamp(move.x, -1f, 1f);
        float throttle = Mathf.Clamp(move.y, -1f, 1f);

        float dt = Time.fixedDeltaTime;

        // Speed (no physics sliding)
        float targetSpeed = throttle * maxSpeed;

        if (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed))
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * dt);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, decel * dt);

        // Turning scales with speed (more stable)
        float speed01 = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(currentSpeed));
        float turnRate = Mathf.Lerp(turnSpeedAtZero, turnSpeedDeg, speed01);

        Quaternion deltaRot = Quaternion.Euler(0f, steer * turnRate * dt, 0f);
        rb.MoveRotation(rb.rotation * deltaRot);

        // Move forward along facing direction
        Vector3 forward = rb.rotation * Vector3.forward;
        Vector3 newPos = rb.position + forward * (currentSpeed * dt);
        rb.MovePosition(newPos);

        // Ensure rigidbody velocity doesn't accumulate
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
