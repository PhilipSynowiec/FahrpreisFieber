using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerInputSystem : MonoBehaviour
{
    [Header("Speed & Power")]
    [SerializeField] private float accelerationForce = 2000f;
    [SerializeField] private float brakingForce = 1500f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float reverseSpeed = 10f;

    [Header("Steering")]
    [SerializeField] private float turnSpeed = 150f;
    [SerializeField] private float grip = 0.95f; // 0 = ice, 1 = rails

    [Header("Physics")]
    [SerializeField] private Transform centerOfMass;

    [SerializeField] private MobileCarInput mobileInput;


    private Rigidbody rb;
    private InputAction moveAction;
    private float currentSteerInput;
    private float currentThrottleInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // We keep gravity off as per original design, or we can turn it on if we want jumps. 
                               // Original had useGravity = false. Let's stick to that for now to avoid falling through world if not set up.
                               // Actually, for a physics car, gravity is usually good, but if the map is flat and we want arcade feel, false is fine.
                               // Let's keep it false but ensure we don't float away.
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Freeze rotation on X and Z to prevent flipping over, allow Y for steering
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

        if (centerOfMass != null)
        {
            rb.centerOfMass = centerOfMass.localPosition;
        }

        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");

        // WASD
        var wasd = moveAction.AddCompositeBinding("2DVector");
        wasd.With("Up", "<Keyboard>/w");
        wasd.With("Down", "<Keyboard>/s");
        wasd.With("Left", "<Keyboard>/a");
        wasd.With("Right", "<Keyboard>/d");

        // Arrows
        var arrows = moveAction.AddCompositeBinding("2DVector");
        arrows.With("Up", "<Keyboard>/upArrow");
        arrows.With("Down", "<Keyboard>/downArrow");
        arrows.With("Left", "<Keyboard>/leftArrow");
        arrows.With("Right", "<Keyboard>/rightArrow");

        // Gamepad
        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    private void OnEnable() => moveAction.Enable();
    private void OnDisable() => moveAction.Disable();

    private void FixedUpdate()
    {
        ReadInput();
        ApplyMovement();
        ApplySteering();
        ApplyLateralFriction();
    }

    private void ReadInput()
    {
        Vector2 move;

        if (mobileInput != null && Application.isMobilePlatform)
        {
            move = new Vector2(mobileInput.Steer, mobileInput.Throttle);
        }
        else
        {
            move = moveAction.ReadValue<Vector2>();
        }

        currentSteerInput = move.x;
        currentThrottleInput = move.y;
    }

    private void ApplyMovement()
    {
        // Calculate speed in the forward direction
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // Determine if we are accelerating, braking, or reversing
        float forceToApply = 0f;

        if (currentThrottleInput > 0)
        {
            // Accelerating forward
            if (forwardSpeed < maxSpeed)
            {
                forceToApply = currentThrottleInput * accelerationForce;
            }
        }
        else if (currentThrottleInput < 0)
        {
            // Reversing or Braking
            if (forwardSpeed > 0.1f)
            {
                // Braking while moving forward
                forceToApply = currentThrottleInput * brakingForce;
            }
            else if (forwardSpeed > -reverseSpeed)
            {
                // Reversing
                forceToApply = currentThrottleInput * accelerationForce;
            }
        }
        else
        {
            // No input, apply drag/coasting logic if needed, or just let physics drag handle it
            // For snappier stop, we can add auto-braking here
             if (Mathf.Abs(forwardSpeed) > 0.1f)
             {
                 forceToApply = -Mathf.Sign(forwardSpeed) * (brakingForce * 0.2f); // Light drag
             }
        }

        rb.AddRelativeForce(Vector3.forward * forceToApply * Time.fixedDeltaTime, ForceMode.Acceleration);
    }

    private void ApplySteering()
    {
        // Only steer if we are moving
        float minSpeedToTurn = 0.1f;
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / 2f); // Full turning at 2 m/s

        if (rb.linearVelocity.magnitude > minSpeedToTurn)
        {
            // Reverse steering when going backward feels more natural for cars
            float directionMult = 1f;
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (forwardSpeed < -0.1f) directionMult = -1f;

            float turn = currentSteerInput * turnSpeed * speedFactor * directionMult * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }

    private void ApplyLateralFriction()
    {
        // Kill sideways velocity to prevent drifting like a hovercraft
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        
        // Apply grip: keep X velocity (sideways) close to 0
        float lateralSpeed = localVelocity.x;
        float frictionForce = -lateralSpeed * grip / Time.fixedDeltaTime;
        
        // Apply as acceleration so it's mass-independent
        Vector3 impulse = transform.right * frictionForce * Time.fixedDeltaTime;
        rb.AddForce(impulse, ForceMode.VelocityChange);
    }
}
