using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerInputSystem : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float turnSpeedDeg = 140f;

    private Rigidbody rb;

    // New Input System actions (no .inputactions asset needed)
    private InputAction moveAction; // Vector2: x=turn, y=forward

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;

        // Composite bindings: WASD + Arrow Keys + Gamepad left stick
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");

        // WASD
        var wasd = moveAction.AddCompositeBinding("2DVector");
        wasd.With("Up", "<Keyboard>/w");
        wasd.With("Down", "<Keyboard>/s");
        wasd.With("Left", "<Keyboard>/a");
        wasd.With("Right", "<Keyboard>/d");

        // Arrow keys
        var arrows = moveAction.AddCompositeBinding("2DVector");
        arrows.With("Up", "<Keyboard>/upArrow");
        arrows.With("Down", "<Keyboard>/downArrow");
        arrows.With("Left", "<Keyboard>/leftArrow");
        arrows.With("Right", "<Keyboard>/rightArrow");

        // Gamepad stick (optional)
        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }

    private void FixedUpdate()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        float turn = Mathf.Clamp(move.x, -1f, 1f);
        float forward = Mathf.Clamp(move.y, -1f, 1f);

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
