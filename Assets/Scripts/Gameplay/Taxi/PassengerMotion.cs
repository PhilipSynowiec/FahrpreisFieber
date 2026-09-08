using UnityEngine;

// Animates the authored prefab joints. No geometry or materials are created at runtime.
public class PassengerMotion : MonoBehaviour
{
    [SerializeField] private Transform body;
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private Transform leftLeg;
    [SerializeField] private Transform rightLeg;
    [SerializeField] private float hailDistance = 15f;

    private Transform taxi;
    private float phase;
    private Quaternion facing;
    private Vector3 headPosition;

    public void Initialize(Transform taxiTarget)
    {
        taxi = taxiTarget;
        phase = Random.value * Mathf.PI * 2f;
        facing = transform.rotation;
        if (head != null) headPosition = head.localPosition;
    }

    private void Update()
    {
        if (body == null) return;
        float t = Time.time + phase;
        Vector3 toTaxi = taxi != null ? taxi.position - transform.position : Vector3.zero;
        toTaxi.y = 0f;
        bool hailing = taxi != null && toTaxi.sqrMagnitude < hailDistance * hailDistance && toTaxi.sqrMagnitude > 0.01f;
        Quaternion desired = hailing ? Quaternion.LookRotation(toTaxi) : facing;
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-3f * Time.deltaTime));

        // A small weight shift stays on the pavement; no uncontrolled wandering into traffic.
        float step = hailing ? 0f : Mathf.Sin(t * 1.1f);
        body.localPosition = new Vector3(step * 0.18f, Mathf.Sin(t * 2.2f) * 0.015f, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.4f) * 2f);
        if (head != null)
        {
            head.localPosition = headPosition;
            head.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.7f) * 4f, hailing ? 0f : Mathf.Sin(t * 0.65f) * 18f, 0f);
        }
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(step * 8f, 0f, 0f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-step * 8f, 0f, 0f);
        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-step * 9f, 0f, -8f);
        if (rightArm != null) rightArm.localRotation = hailing
            ? Quaternion.Euler(0f, Mathf.Sin(t * 7f) * 20f, 145f + Mathf.Sin(t * 7f) * 14f)
            : Quaternion.Euler(step * 9f, 0f, 8f);
    }
}
