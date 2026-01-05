using UnityEngine;
using UnityEngine.UI;

public class TargetArrowUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TripManager trips;
    [SerializeField] private TileStreamingManager world;
    [SerializeField] private Transform car;

    [Header("UI")]
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private Image arrowImage;

    [Header("Behavior")]
    [SerializeField] private bool hideWhenNoTrip = true;
    [SerializeField] private float smooth = 12f;

    [Header("Sprite alignment")]
    [Tooltip("If your arrow graphic is rotated in the editor, compensate here (degrees).")]
    [SerializeField] private float angleOffsetDeg = -45f;

    private float currentAngle;

    private void Awake()
    {
        if (trips == null) trips = FindFirstObjectByType<TripManager>();
        if (world == null) world = FindFirstObjectByType<TileStreamingManager>();
        if (car == null)
        {
            var c = FindFirstObjectByType<CarControllerInputSystem>();
            if (c != null) car = c.transform;
        }
    }

    private void Update()
    {
        if (arrowRect == null || trips == null || world == null || car == null) return;

        bool hasTrip = trips.HasActiveTrip;

        if (hideWhenNoTrip)
        {
            if (arrowImage != null) arrowImage.enabled = hasTrip;
            if (!hasTrip) return;
        }

        Vector2Int destGrid = trips.ActiveJob.destinationBuilding;
        Vector3 destWorld = new Vector3(destGrid.x * world.TileSize, 0f, destGrid.y * world.TileSize);
        Vector3 from = new Vector3(car.position.x, 0f, car.position.z);

        Vector3 dir = destWorld - from;
        if (dir.sqrMagnitude < 0.0001f) return;

        // 0° should mean "up" in UI; in world, +Z is "up"
        float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + angleOffsetDeg;

        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        arrowRect.localEulerAngles = new Vector3(0f, 0f, -currentAngle);
    }
}
