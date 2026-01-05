using UnityEngine;

public class TaxiInteractionController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody carRb;
    [SerializeField] private MonoBehaviour carControllerToDisable; // CarControllerInputSystem
    [SerializeField] private TaxiHUDUI hud;
    [SerializeField] private PassengerSpawner spawner;
    [SerializeField] private TripManager trips;

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
    }

    private void Update()
    {
        if (hud == null || trips == null) return;

        bool stopped = IsStopped();

        // ACTIVE TRIP: reuse same HUD button for dropoff (only when near + stopped)
        if (trips.HasActiveTrip)
        {
            ClearNearbyOffer();

            bool canDrop = trips.IsNearDestination(transform.position) && stopped;
            hud.SetAccept(visible: true, enabled: canDrop, label: dropoffLabel, onClick: () =>
            {
                if (!canDrop) return;
                FreezeCar(true);
                var result = trips.CompleteTrip(transform.position);
                // If you still have the left result popup, keep it. Otherwise just unfreeze.
                FreezeCar(false);
            });

            return;
        }

        // NO TRIP: show offer widget above closest passenger + enable HUD accept when close+stopped
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

    private void OnAccept()
    {
        if (nearbyPassenger == null || trips == null) return;
        if (!IsStopped()) return;

        trips.StartTrip(nearbyPassenger.Job);

        if (spawner != null) spawner.DespawnPassenger(nearbyPassenger);
        else Destroy(nearbyPassenger.gameObject);

        ClearNearbyOffer();
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
