using UnityEngine;

public class TaxiInteractionController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody carRb;
    [SerializeField] private TaxiHUDUI hud;
    [SerializeField] private PassengerSpawner spawner;
    [SerializeField] private TripManager trips;

    [Header("UI")]
    [SerializeField] private ToastMessageUI toast;

    [Header("Offer Widget Prefab")]
    [SerializeField] private PassengerOfferWidget offerWidgetPrefab;

    [Header("Pickup Detection")]
    [SerializeField] private float detectRadius = 2.5f;
    [SerializeField] private LayerMask passengerLayer = ~0;
    [SerializeField, Min(0f)] private float pickupSpeedThreshold = 4f;
    [SerializeField, Min(0f)] private float dropoffSpeedThreshold = 4f;

    private Passenger nearbyPassenger;
    private PassengerOfferWidget nearbyWidget;

    private void Awake()
    {
        if (carRb == null) carRb = GetComponent<Rigidbody>();
        if (hud == null) hud = FindFirstObjectByType<TaxiHUDUI>();
        if (spawner == null) spawner = FindFirstObjectByType<PassengerSpawner>();
        if (trips == null) trips = FindFirstObjectByType<TripManager>();
        if (toast == null) toast = FindFirstObjectByType<ToastMessageUI>();
        if (reviewService == null) reviewService = FindFirstObjectByType<OfflineReviewService>();
    }

    private void Update()
    {
        if (hud == null || trips == null) return;

        bool canBoardAtSpeed = IsWithinSpeed(pickupSpeedThreshold);

        // ACTIVE TRIP: use HUD button for dropoff
        if (trips.HasActiveTrip)
        {
            ClearNearbyOffer();

            bool canDrop = trips.IsNearDestination(transform.position) && IsWithinSpeed(dropoffSpeedThreshold);
            hud.SetAccept(visible: true, enabled: canDrop, droppingOff: true, onClick: () =>
            {
                if (!canDrop) return;
                DoDropoff();
            });

            return;
        }

        // NO TRIP: show offer widget + HUD accept
        var p = FindNearestPassenger();
        if (p != nearbyPassenger)
            SetNearbyPassenger(p);

        bool canAccept = (nearbyPassenger != null) && canBoardAtSpeed;
        hud.SetAccept(visible: true, enabled: canAccept, droppingOff: false, onClick: () =>
        {
            if (!canAccept) return;
            OnAccept();
        });
    }

    private bool droppingOff;

    private void OnAccept()
    {
        if (nearbyPassenger == null || trips == null) return;
        if (!IsWithinSpeed(pickupSpeedThreshold)) return;

        droppingOff = false; // Reset for the new trip
        trips.StartTrip(nearbyPassenger.Job);

        if (spawner != null) spawner.DespawnPassenger(nearbyPassenger);
        else Destroy(nearbyPassenger.gameObject);

        ClearNearbyOffer();
    }

    [SerializeField] private OfflineReviewService reviewService;

    private void DoDropoff()
    {
        // Recheck at click time, not just when the HUD last refreshed.
        if (trips == null || !trips.HasActiveTrip || droppingOff || !trips.IsNearDestination(transform.position)
            || !IsWithinSpeed(dropoffSpeedThreshold)) return;
        droppingOff = true;

        TripResult result = trips.CompleteTrip(transform.position);
        
        // Calculate trip performance for the review service
        float ratio = 0f;
        if (result.timeLimitSeconds > 0)
            ratio = (result.timeLimitSeconds - result.elapsedSeconds) / result.timeLimitSeconds;

        int stars = ReviewRating.FromTimeRatio(ratio);
        string review = reviewService != null ? reviewService.GetReview(ratio)
            : PassengerReviewCatalog.ByStars[stars - 1][0];
        if (toast != null) toast.ShowReview(review, stars, result.payoutCoins);
    }

    private bool IsWithinSpeed(float threshold)
    {
        if (carRb == null) return true;
        Vector3 v = carRb.linearVelocity; v.y = 0f;
        return v.sqrMagnitude <= threshold * threshold;
    }

    private Passenger FindNearestPassenger()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, passengerLayer, QueryTriggerInteraction.Collide);
        Passenger best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            var p = hits[i].GetComponentInParent<Passenger>();
            if (p == null) continue;

            float d = (p.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }

    private void SetNearbyPassenger(Passenger p)
    {
        ClearNearbyOffer();
        nearbyPassenger = p;
        if (nearbyPassenger == null || offerWidgetPrefab == null) return;

        nearbyWidget = Instantiate(offerWidgetPrefab);
        nearbyWidget.name = "OfferWidget";
        nearbyWidget.Bind(nearbyPassenger.transform, nearbyPassenger.Job);
    }

    private void ClearNearbyOffer()
    {
        nearbyPassenger = null;
        if (nearbyWidget != null) Destroy(nearbyWidget.gameObject);
        nearbyWidget = null;
    }

}
