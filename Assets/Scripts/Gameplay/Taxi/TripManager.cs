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
    public float RemainingSeconds => activeJob == null ? 0f : Mathf.Max(0f, activeJob.timeLimitSeconds - (Time.time - tripStartTime));

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
    {
        // Resolve the persistent manager when a scene is enabled.
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
        Vector3 dest = activeJob.destinationWorld;
        return Vector3.Distance(new Vector3(carPos.x, 0f, carPos.z), dest) <= arriveDistanceWorld;
    }

    public TripResult CompleteTrip(Vector3 carPos)
    {
        if (activeJob == null) return default;

        // ensure currency is resolved even if scene changed
        if (currency == null)
            currency = CurrencyManager.Instance != null ? CurrencyManager.Instance : FindFirstObjectByType<CurrencyManager>();

        float elapsed = Time.time - tripStartTime;
        var result = TripRewardCalculator.Complete(activeJob, elapsed, IsNearDestination(carPos));
        int payout = result.payoutCoins;

        if (payout > 0)
        {
            if (currency != null) currency.AddCoins(payout);
            else Debug.LogWarning("TripManager: CurrencyManager missing; payout not applied.");
        }

        if (speedModel != null)
            speedModel.ReportTripResult(elapsed, activeJob.timeLimitSeconds);

        activeJob = null;
        DespawnMarker();
        return result;
    }

    private void SpawnMarker()
    {
        DespawnMarker();
        if (destinationMarkerPrefab == null || world == null || activeJob == null) return;

        Vector3 pos = activeJob.destinationWorld + new Vector3(0f, 0.7f, 0f);
        markerInstance = Instantiate(destinationMarkerPrefab, pos, Quaternion.identity);
        markerInstance.name = "DestinationMarker";
    }

    private void DespawnMarker()
    {
        if (markerInstance != null) Destroy(markerInstance);
        markerInstance = null;
    }

}

public struct TripResult
{
    public bool onTime;
    public float elapsedSeconds;
    public float timeLimitSeconds;
    public int payoutCoins;
}
