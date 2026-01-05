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
    [SerializeField] private float turnSpeedDeg = 180f;
    [SerializeField] private float turnSpeedAtZero = 60f;

    [Header("Anti-slide")]
    [SerializeField] private float lateralFriction = 12f;

    [Header("Collision (reliable)")]
    [SerializeField] private LayerMask solidMask = ~0;
    [SerializeField] private float skin = 0.03f;
    [SerializeField] private int depenetrationIters = 3;

    private Rigidbody rb;
    private Collider carCol;

    private InputAction moveAction;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // IMPORTANT: allow collider on child (very common setup)
        carCol = GetComponent<Collider>();
        if (carCol == null) carCol = GetComponentInChildren<Collider>();

        if (carCol == null)
            Debug.LogError("CarControllerInputSystem: No Collider found on car (root or children).");

        rb.useGravity = false;
        rb.isKinematic = false;

        rb.constraints = RigidbodyConstraints.FreezePositionY |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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

        float targetSpeed = throttle * maxSpeed;
        float rate = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)) ? accel : decel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * dt);

        float speed01 = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(currentSpeed));
        float turnRate = Mathf.Lerp(turnSpeedAtZero, turnSpeedDeg, speed01);

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steer * turnRate * dt, 0f));

        // Depenetrate BEFORE move (safe)
        Depenetrate();

        Vector3 forward = rb.rotation * Vector3.forward;
        Vector3 delta = forward * (currentSpeed * dt);

        SafeMove(delta);

        // Depenetrate AFTER move (safe)
        Depenetrate();

        // Lateral damping
        Vector3 v = rb.linearVelocity;
        Vector3 right = rb.rotation * Vector3.right;
        float lateral = Vector3.Dot(v, right);
        v -= right * lateral * Mathf.Clamp01(lateralFriction * dt);
        rb.linearVelocity = v;
    }

    private void SafeMove(Vector3 delta)
    {
        if (carCol == null) { rb.MovePosition(rb.position + delta); return; }
        if (delta.sqrMagnitude < 1e-10f) return;

        Vector3 pos = rb.position;
        Vector3 dir = delta.normalized;
        float dist = delta.magnitude;

        if (CastAlong(dir, dist + skin, out RaycastHit hit))
        {
            float allowed = Mathf.Max(0f, hit.distance - skin);

            // Stop pushing into the wall
            if (allowed <= 0.001f) currentSpeed = 0f;

            rb.MovePosition(pos + dir * allowed);
        }
        else
        {
            rb.MovePosition(pos + delta);
        }
    }

    private bool CastAlong(Vector3 dir, float dist, out RaycastHit hit)
    {
        hit = default;
        const QueryTriggerInteraction q = QueryTriggerInteraction.Ignore;

        if (carCol is CapsuleCollider cap)
        {
            GetCapsuleWorld(cap, out Vector3 p1, out Vector3 p2, out float r);
            return Physics.CapsuleCast(p1, p2, r, dir, out hit, dist, solidMask, q);
        }

        if (carCol is BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 half = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            return Physics.BoxCast(center, half, dir, out hit, box.transform.rotation, dist, solidMask, q);
        }

        // Fallback: bounds cast by sphere (conservative)
        float r0 = Mathf.Max(0.25f, Mathf.Min(carCol.bounds.extents.x, carCol.bounds.extents.z));
        Vector3 c = carCol.bounds.center;
        return Physics.SphereCast(c, r0, dir, out hit, dist, solidMask, q);
    }

    private void Depenetrate()
    {
        if (carCol == null) return;

        for (int iter = 0; iter < depenetrationIters; iter++)
        {
            Bounds b = carCol.bounds;
            Collider[] overlaps = Physics.OverlapBox(
                b.center, b.extents, carCol.transform.rotation,
                solidMask, QueryTriggerInteraction.Ignore);

            bool moved = false;

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider other = overlaps[i];
                if (other == null || other == carCol) continue;

                if (Physics.ComputePenetration(
                        carCol, carCol.transform.position, carCol.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir, out float distance))
                {
                    float push = distance + skin;
                    rb.MovePosition(rb.position + dir * push);
                    moved = true;
                }
            }

            if (!moved) break;
        }
    }

    private void GetCapsuleWorld(CapsuleCollider cap, out Vector3 p1, out Vector3 p2, out float radius)
    {
        Transform t = cap.transform;
        Vector3 center = t.TransformPoint(cap.center);

        Vector3 s = t.lossyScale;
        float rScale = Mathf.Max(s.x, s.z);
        radius = Mathf.Max(0.01f, cap.radius * rScale);

        float height = Mathf.Max(radius * 2f, cap.height * s.y);
        float half = (height * 0.5f) - radius;

        Vector3 up = t.up;
        p1 = center + up * half;
        p2 = center - up * half;
    }
}
