using UnityEngine;

public class Passenger : MonoBehaviour
{
    public PassengerJob Job { get; private set; }
    public Vector2Int SpawnEdgeBuilding { get; private set; }
    public Vector2Int SpawnEdgeDir { get; private set; } // (1,0),(-1,0),(0,1),(0,-1)

    private float despawnAt;

    public void Init(PassengerJob job, Vector2Int building, Vector2Int dir, float lifetimeSeconds)
    {
        Job = job;
        SpawnEdgeBuilding = building;
        SpawnEdgeDir = dir;
        despawnAt = Time.time + lifetimeSeconds;
    }

    public bool IsExpired => Time.time >= despawnAt;
}
