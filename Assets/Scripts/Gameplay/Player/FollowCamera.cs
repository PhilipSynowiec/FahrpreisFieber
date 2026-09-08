using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 18, -10.4f);
    [SerializeField] private Vector3 driverOffset = new Vector3(0, .42f, .25f);
    [SerializeField] private Vector3 chaseOffset = new Vector3(0, 3f, -6f);
    private Quaternion elevatedRotation;
    private Renderer[] carRenderers;
    private Camera viewCamera;
    private float elevatedFov;
    private DrivingView mode;
    private bool initialized;

    private void Awake()
    {
        elevatedRotation = transform.rotation;
        viewCamera = GetComponent<Camera>();
        elevatedFov = viewCamera.fieldOfView;
        carRenderers = target.GetComponentsInChildren<Renderer>(true);
        viewCamera.nearClipPlane = .08f;
        initialized = true;
        SetView(DrivingViewPreference.Selected);
    }
    public void SetView(DrivingView view)
    {
        mode = view;
        if (!initialized) return;
        foreach (var renderer in carRenderers) if (renderer != null) renderer.forceRenderingOff = mode == DrivingView.Driver;
        viewCamera.fieldOfView = mode == DrivingView.Driver ? 75 : mode == DrivingView.Chase ? 65 : elevatedFov;
        UpdatePose(true);
    }
    private void LateUpdate() { if (initialized) UpdatePose(false); }
    private void OnDisable()
    {
        if (carRenderers != null) foreach (var renderer in carRenderers) if (renderer != null) renderer.forceRenderingOff = false;
    }
    private void OnEnable() { if (initialized) SetView(mode); }
    private void UpdatePose(bool snap)
    {
        if (target == null) return;
        if (mode == DrivingView.Elevated)
        {
            transform.SetPositionAndRotation(target.position + offset, elevatedRotation);
            return;
        }
        if (mode == DrivingView.Driver)
        {
            transform.SetPositionAndRotation(target.TransformPoint(driverOffset), target.rotation);
            return;
        }
        Vector3 focus = target.position + Vector3.up + target.forward * 1.5f;
        Vector3 desired = target.TransformPoint(chaseOffset);
        Vector3 delta = desired-focus; float distance=delta.magnitude;
        // Pull the chase camera forward when a building blocks the view; ignore the taxi.
        foreach (var hit in Physics.SphereCastAll(focus, .25f, delta.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(target)) distance=Mathf.Min(distance,Mathf.Max(.3f,hit.distance-.15f));
        desired=focus+delta.normalized*distance;
        float blend=snap?1f:1f-Mathf.Exp(-10f*Time.deltaTime);
        // Immediately obey occlusion instead of interpolating through a wall.
        transform.position=distance<delta.magnitude?desired:Vector3.Lerp(transform.position,desired,blend);
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(focus-transform.position,Vector3.up),blend);
    }
}
