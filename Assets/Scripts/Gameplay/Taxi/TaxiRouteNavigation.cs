using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TaxiRouteNavigation : MonoBehaviour
{
    [SerializeField] private TileStreamingManager world;
    [SerializeField] private Transform car;
    [SerializeField] private TripManager trips;
    private readonly List<Vector2Int> route = new();
    public IReadOnlyList<Vector2Int> Route => route;
    public int Revision { get; private set; }
    private PassengerJob lastJob;
    private Vector2Int lastStart, lastGoal;
    private int lastSeed;
    private float nextCheck;

    private void Update()
    {
        if (Application.isPlaying && Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.1f;
        RefreshRoute();
    }

    public void RefreshRoute()
    {
        if (world == null || car == null || trips == null || !trips.HasActiveTrip)
        {
            if (route.Count > 0) { route.Clear(); Revision++; }
            lastJob = null;
            return;
        }
        Vector2Int start = world.WorldToGrid(car.position);
        Vector2Int goal = world.WorldToGrid(trips.ActiveJob.destinationWorld);
        if (lastJob == trips.ActiveJob && start == lastStart && goal == lastGoal && lastSeed == world.WorldSeed) return;
        lastJob = trips.ActiveJob; lastStart = start; lastGoal = goal; lastSeed = world.WorldSeed;
        var from = CityLayout.NearestRoad(lastSeed, start.x, start.y);
        var to = CityLayout.NearestRoad(lastSeed, goal.x, goal.y);
        var path = CityLayout.FindRoadPath(lastSeed, from.x, from.y, to.x, to.y);
        route.Clear();
        foreach (var point in path) route.Add(new Vector2Int(point.x, point.y));
        Revision++;
    }
}
