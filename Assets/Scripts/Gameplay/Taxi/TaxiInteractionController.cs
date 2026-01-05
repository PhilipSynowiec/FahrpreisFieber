using UnityEngine;

public class TaxiInteractionController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody carRb;
    [SerializeField] private MonoBehaviour carControllerToDisable; // CarControllerInputSystem
    [SerializeField] private TaxiHUDUI hud;
    [SerializeField] private PassengerSpawner spawner;
    [SerializeField] private TripManager trips;

    [Header("UI")]
    [SerializeField] private ToastMessageUI toast;

    [Header("Offer Widget Prefab (World Space, display only)")]
    [SerializeField] private PassengerOfferWidget offerWidgetPrefab;

    [Header("Pickup Detection")]
    [SerializeField] private float detectRadius = 2.5f;
    [SerializeField] private LayerMask passengerLayer = ~0;
    [SerializeField] private float stopSpeedThreshold = 0.2f;

    [Header("HUD Labels")]
    [SerializeField] private string acceptLabel = "Accept";
    [SerializeField] private string dropoffLabel = "Dropoff";

    private Passenger nearbyPassenger;
    private PassengerOfferWidget nearbyWidget;

    private void Awake()
    {
        if (carRb == null) carRb = GetComponent<Rigidbody>();
        if (hud == null) hud = FindFirstObjectByType<TaxiHUDUI>();
        if (spawner == null) spawner = FindFirstObjectByType<PassengerSpawner>();
        if (trips == null) trips = FindFirstObjectByType<TripManager>();
        if (toast == null) toast = FindFirstObjectByType<ToastMessageUI>();
        if (reviewService == null) reviewService = FindFirstObjectByType<GeminiReviewService>();
    }

    private void Update()
    {
        if (hud == null || trips == null) return;

        bool stopped = IsStopped();

        // ACTIVE TRIP: use HUD button for dropoff
        if (trips.HasActiveTrip)
        {
            ClearNearbyOffer();

            bool canDrop = trips.IsNearDestination(transform.position) && stopped;
            hud.SetAccept(visible: true, enabled: canDrop, label: dropoffLabel, onClick: () =>
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

        bool canAccept = (nearbyPassenger != null) && stopped;
        hud.SetAccept(visible: true, enabled: canAccept, label: acceptLabel, onClick: () =>
        {
            if (!canAccept) return;
            OnAccept();
        });
    }

    private bool droppingOff;

    private void OnAccept()
    {
        if (nearbyPassenger == null || trips == null) return;
        if (!IsStopped()) return;

        droppingOff = false; // Reset for the new trip
        trips.StartTrip(nearbyPassenger.Job);

        if (spawner != null) spawner.DespawnPassenger(nearbyPassenger);
        else Destroy(nearbyPassenger.gameObject);

        ClearNearbyOffer();
    }

    [SerializeField] private GeminiReviewService reviewService;

    private void DoDropoff()
    {
        if (trips == null || droppingOff) return;
        droppingOff = true;

        FreezeCar(true);
        TripResult result = trips.CompleteTrip(transform.position);
        
        // Calculate performance ratio for the AI
        float ratio = 0f;
        if (result.timeLimitSeconds > 0)
            ratio = (result.timeLimitSeconds - result.elapsedSeconds) / result.timeLimitSeconds;

        if (reviewService != null)
        {
            // Show "Generating..." or similar if desired, or just wait
            // if (toast != null) toast.Show("Asking passenger for review...");

            reviewService.GenerateReview(ratio, result.onTime, (review) =>
            {
                FreezeCar(false);
                if (toast != null)
                {
                    string header = result.onTime ? $"Delivered! (+{result.payoutCoins})" : "Late! (+0)";
                    toast.Show($"{header}\n\"{review}\"");
                }
            });
        }
        else
        {
            // Fallback if service not assigned
            FreezeCar(false);
            if (toast != null)
            {
                string review = result.onTime ? "Good job!" : "Too slow!";
                string header = result.onTime ? $"Delivered! (+{result.payoutCoins})" : "Late! (+0)";
                toast.Show($"{header}\nReview: {review}");
            }
        }
    }

    private bool IsStopped()
    {
        if (carRb == null) return true;
        Vector3 v = carRb.linearVelocity; v.y = 0f;
        return v.magnitude <= stopSpeedThreshold;
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

    private void FreezeCar(bool freeze)
    {
        if (carControllerToDisable != null)
            carControllerToDisable.enabled = !freeze;

        if (carRb != null && freeze)
        {
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
        }
    }
}
