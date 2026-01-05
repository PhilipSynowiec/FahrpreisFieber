using UnityEngine;

public class TripManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TileStreamingManager world;
    [SerializeField] private RatingSpeedModel speedModel;

    private CurrencyManager currency;

    [Header("Arrival")]
    [SerializeField] private float arriveDistanceWorld = 2.5f;

    [Header("Marker (optional)")]
    [SerializeField] private GameObject destinationMarkerPrefab;

    public bool HasActiveTrip => activeJob != null;
    public PassengerJob ActiveJob => activeJob;

    private PassengerJob activeJob;
    private float tripStartTime;
    private GameObject markerInstance;

    private void Awake()
    {
        if (world == null) world = FindFirstObjectByType<TileStreamingManager>();
        if (speedModel == null) speedModel = FindFirstObjectByType<RatingSpeedModel>();

        // Auto-resolve currency
        if (currency == null)
            currency = CurrencyManager.Instance != null ? CurrencyManager.Instance : FindFirstObjectByType<CurrencyManager>();
    }

    private void OnEnable()
    {        // In case CurrencyManager is created later (DDOL)
        if (currency == null)
            currency = CurrencyManager.Instance != null ? CurrencyManager.Instance : FindFirstObjectByType<CurrencyManager>();
    }

    public void StartTrip(PassengerJob job)
    {
        activeJob = job;
        tripStartTime = Time.time;
        SpawnMarker();
    }

    public bool IsNearDestination(Vector3 carPos)
    {
        if (activeJob == null || world == null) return false;
        Vector3 dest = GridToWorld(activeJob.destinationBuilding);
        return Vector3.Distance(new Vector3(carPos.x, 0f, carPos.z), dest) <= arriveDistanceWorld;
    }

    public TripResult CompleteTrip(Vector3 carPos)
    {
        if (activeJob == null) return default;

        // ensure currency is resolved even if scene changed
        if (currency == null)
            currency = CurrencyManager.Instance != null ? CurrencyManager.Instance : FindFirstObjectByType<CurrencyManager>();

        float elapsed = Time.time - tripStartTime;
        bool reached = IsNearDestination(carPos);
        bool onTime = reached && elapsed <= activeJob.timeLimitSeconds;

        float achievedAvgSpeed = activeJob.distanceWorld / Mathf.Max(0.01f, elapsed);
        string rating = onTime ? "gut" : "schlecht";
        int payout = onTime ? activeJob.quotedPriceCoins : 0;

        if (payout > 0)
        {
            if (currency != null) currency.AddCoins(payout);
            else Debug.LogWarning("TripManager: CurrencyManager missing; payout not applied.");
        }

        if (speedModel != null)
            speedModel.ReportTripResult(elapsed, activeJob.timeLimitSeconds);

        var result = new TripResult
        {
            reached = reached,
            onTime = onTime,
            elapsedSeconds = elapsed,
            timeLimitSeconds = activeJob.timeLimitSeconds,
            requiredAvgSpeed = activeJob.requiredAvgSpeed,
            achievedAvgSpeed = achievedAvgSpeed,
            payoutCoins = payout,
            rating = rating
        };

        activeJob = null;
        DespawnMarker();
        return result;
    }

    private void SpawnMarker()
    {
        DespawnMarker();
        if (destinationMarkerPrefab == null || world == null || activeJob == null) return;

        Vector3 pos = GridToWorld(activeJob.destinationBuilding) + new Vector3(0f, 0.7f, 0f);
        markerInstance = Instantiate(destinationMarkerPrefab, pos, Quaternion.identity);
        markerInstance.name = "DestinationMarker";
    }

    private void DespawnMarker()
    {
        if (markerInstance != null) Destroy(markerInstance);
        markerInstance = null;
    }

    private Vector3 GridToWorld(Vector2Int g) => new Vector3(g.x * world.TileSize, 0f, g.y * world.TileSize);
}

public struct TripResult
{
    public bool reached;
    public bool onTime;
    public float elapsedSeconds;
    public float timeLimitSeconds;
    public float requiredAvgSpeed;
    public float achievedAvgSpeed;
    public int payoutCoins;
    public string rating;
}
