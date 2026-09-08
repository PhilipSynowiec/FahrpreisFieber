using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarControllerInputSystem : MonoBehaviour
{
    [Header("Arcade movement")]
    [SerializeField, Min(0f)] private float acceleration = 12f;
    [SerializeField, Min(0f)] private float brakeDeceleration = 20f;
    [SerializeField, Min(0f)] private float maxSpeed = 20f;
    [SerializeField, Min(0f)] private float reverseSpeed = 10f;
    [SerializeField, Min(0f)] private float reverseDelay = 0.4f;

    [Header("Steering and controlled drift")]
    [SerializeField, Min(0.1f)] private float wheelbase = 1.8f;
    [SerializeField, Range(1f, 60f)] private float maxSteeringAngle = 30f;
    [SerializeField, Min(0.1f)] private float maxLateralAcceleration = 96f;
    [SerializeField, Min(0f)] private float steerResponse = 4f;
    [SerializeField, Min(0f)] private float lateralGrip = 12f;
    [SerializeField, Min(0f)] private float maxSidewaysSpeed = 2f;

    [SerializeField] private Transform centerOfMass;
    [SerializeField] private MobileCarInput mobileInput;
    private Rigidbody rb;
    private InputAction moveAction;
    private float steer;
    private float stoppedBrakeTime;

    public void SetTopSpeed(float speed) => maxSpeed = Mathf.Max(0.1f, speed);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Collision resolution still controls translation. Only input may turn the taxi.
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.linearDamping = 0f;
        rb.angularVelocity = Vector3.zero;
        if (centerOfMass != null) rb.centerOfMass = centerOfMass.localPosition;
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    private void OnEnable() => moveAction?.Enable();
    private void OnDisable() { moveAction?.Disable(); steer = 0f; stoppedBrakeTime = 0f; }
    private void OnDestroy() => moveAction?.Dispose();

    private void FixedUpdate()
    {
        Vector2 input = mobileInput != null && mobileInput.isActiveAndEnabled
            ? new Vector2(mobileInput.Steer, mobileInput.Throttle) : moveAction.ReadValue<Vector2>();
        Drive(input.x, input.y, Time.fixedDeltaTime);
    }

    private void Drive(float steeringInput, float throttle, float dt)
    {
        Vector3 velocity = rb.linearVelocity; velocity.y = 0f;
        Vector3 forward = rb.rotation * Vector3.forward;
        float speed = Vector3.Dot(velocity, forward);
        float nextSpeed;
        if (throttle < 0f)
        {
            if (speed > 0.05f)
            {
                nextSpeed = Mathf.MoveTowards(speed, 0f, brakeDeceleration * dt);
                stoppedBrakeTime = 0f;
            }
            else if (speed >= -0.05f && stoppedBrakeTime < reverseDelay)
            {
                nextSpeed = 0f;
                stoppedBrakeTime += dt;
            }
            else nextSpeed = Mathf.MoveTowards(speed, -reverseSpeed, acceleration * dt);
        }
        else
        {
            stoppedBrakeTime = 0f;
            float target = throttle > 0f ? maxSpeed : 0f;
            nextSpeed = Mathf.MoveTowards(speed, target, (speed < 0f || throttle == 0f ? brakeDeceleration : acceleration) * dt);
        }
        nextSpeed = Mathf.Clamp(nextSpeed, -reverseSpeed, maxSpeed);
        steer = Mathf.MoveTowards(steer, Mathf.Clamp(steeringInput, -1f, 1f), steerResponse * dt);
        // Bicycle model: steering sets wheel angle; motion sets yaw. This gives
        // a stable circular arc under held input and naturally wider fast turns.
        float speedSquared = nextSpeed * nextSpeed;
        float gripAngle = Mathf.Atan(maxLateralAcceleration * wheelbase / Mathf.Max(.01f, speedSquared));
        float wheelAngle = steer * Mathf.Min(maxSteeringAngle * Mathf.Deg2Rad, gripAngle);
        float beta = Mathf.Atan(.5f * Mathf.Tan(wheelAngle));
        float yawRadians = nextSpeed / wheelbase * Mathf.Cos(beta) * Mathf.Tan(wheelAngle) * dt;
        Quaternion rotation = rb.rotation * Quaternion.Euler(0, yawRadians * Mathf.Rad2Deg, 0);
        // Integrate velocity at the middle of the arc, not at its final heading.
        Quaternion travelRotation = rb.rotation * Quaternion.Euler(0, (yawRadians*.5f + beta) * Mathf.Rad2Deg, 0);
        Vector3 right = travelRotation * Vector3.right;
        float slide = Mathf.Clamp(Vector3.Dot(velocity, right) * Mathf.Exp(-lateralGrip * dt), -maxSidewaysSpeed, maxSidewaysSpeed);
        Vector3 nextVelocity = travelRotation * Vector3.forward * nextSpeed + right * slide;
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(rotation);
        rb.linearVelocity = Vector3.ClampMagnitude(nextVelocity, nextSpeed < 0f ? reverseSpeed : maxSpeed);
    }
}
