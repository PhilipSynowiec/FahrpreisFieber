using System.Collections.Generic;
using UnityEngine;

public class PassengerSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TileStreamingManager world;
    [SerializeField] private RatingSpeedModel speedModel;

    [Header("Passenger Prefab")]
    [SerializeField] private Passenger passengerPrefab;

    [Header("Spawn Rules")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnChancePerEdge = 0.08f;

    [SerializeField] private float spawnIntervalSeconds = 15f;
    [SerializeField] private float lifetimeSeconds = 120f;

    [SerializeField] private float standOffFromBuilding = 0.6f;
    [SerializeField] private float alongEdgeRandom = 0.35f;

    [Header("Target Selection")]
    [SerializeField] private int minTargetDistanceTiles = 6;
    [SerializeField] private int maxTargetDistanceTiles = 30;

    [Header("Fare")]
    [SerializeField] private float coinsPerWorldUnit = 1.2f;
    [SerializeField] private float rushCoinsFactor = 8f;

    private float nextSpawnTick;

    private readonly Dictionary<EdgeKey, Passenger> activeByEdge = new();
    private readonly List<EdgeKey> edgeScratch = new();

    private void Awake()
    {
        nextSpawnTick = Time.time + spawnIntervalSeconds;
        if (world == null) world = FindFirstObjectByType<TileStreamingManager>();
        if (speedModel == null) speedModel = FindFirstObjectByType<RatingSpeedModel>();

        if (world != null)
            world.TileUnloaded += OnTileUnloaded;
    }

    private void OnDestroy()
    {
        if (world != null)
            world.TileUnloaded -= OnTileUnloaded;
    }

    private void Update()
    {
        if (world == null || passengerPrefab == null) return;

        // despawn expired
        edgeScratch.Clear();
        foreach (var kv in activeByEdge)
        {
            if (kv.Value == null || kv.Value.IsExpired)
                edgeScratch.Add(kv.Key);
        }
        for (int i = 0; i < edgeScratch.Count; i++)
            Despawn(edgeScratch[i]);

        if (Time.time < nextSpawnTick) return;
        nextSpawnTick = Time.time + spawnIntervalSeconds;

        foreach (var kv in world.EnumerateSpawnedTiles())
        {
            var inst = kv.Value != null ? kv.Value.GetComponent<TileInstance>() : null;
            if (inst == null) continue;
            if (inst.Data.kind != TileKind.Building) continue;

            TrySpawnOnBuildingEdges(inst.GridPos);
        }
    }

    public void DespawnPassenger(Passenger p)
    {
        if (p == null) return;
        var key = new EdgeKey(p.SpawnEdgeBuilding, p.SpawnEdgeDir);
        Despawn(key);
    }

    private void TrySpawnOnBuildingEdges(Vector2Int building)
    {
        TryEdge(building, new Vector2Int(1, 0));
        TryEdge(building, new Vector2Int(-1, 0));
        TryEdge(building, new Vector2Int(0, 1));
        TryEdge(building, new Vector2Int(0, -1));
    }

    private void TryEdge(Vector2Int building, Vector2Int dirToRoad)
    {
        var key = new EdgeKey(building, dirToRoad);
        if (activeByEdge.ContainsKey(key)) return;

        Vector2Int roadPos = building + dirToRoad;
        var roadData = world.GetOrGenerateTileData(roadPos);
        if (roadData.kind != TileKind.Road) return;

        if (Random.value > spawnChancePerEdge) return;

        var job = CreateJob(building);

        Vector3 pos = ComputeSpawnPosition(building, dirToRoad);
        var p = Instantiate(passengerPrefab, pos, Quaternion.identity, transform);
        p.name = $"Passenger_{building.x}_{building.y}_to_{job.destinationBuilding.x}_{job.destinationBuilding.y}";
        p.Init(job, building, dirToRoad, lifetimeSeconds);

        activeByEdge[key] = p;
    }

    private PassengerJob CreateJob(Vector2Int originBuilding)
    {
        Vector2Int dest = PickDestination(originBuilding);

        float distanceWorld = Vector3.Distance(GridToWorld(originBuilding), GridToWorld(dest));
        float sampledSpeed = (speedModel != null) ? speedModel.SampleSpeed() : 6f;

        float timeLimit = Mathf.Clamp(distanceWorld / sampledSpeed, 10f, 240f);
        float requiredAvgSpeed = distanceWorld / timeLimit;

        int price = Mathf.RoundToInt(distanceWorld * coinsPerWorldUnit + requiredAvgSpeed * rushCoinsFactor);

        return new PassengerJob
        {
            originBuilding = originBuilding,
            destinationBuilding = dest,
            timeLimitSeconds = timeLimit,
            distanceWorld = distanceWorld,
            requiredAvgSpeed = requiredAvgSpeed,
            quotedPriceCoins = price
        };
    }

    private Vector2Int PickDestination(Vector2Int origin)
    {
        for (int i = 0; i < 40; i++)
        {
            int dx = Random.Range(-maxTargetDistanceTiles, maxTargetDistanceTiles + 1);
            int dy = Random.Range(-maxTargetDistanceTiles, maxTargetDistanceTiles + 1);
            var g = new Vector2Int(origin.x + dx, origin.y + dy);

            int man = Mathf.Abs(dx) + Mathf.Abs(dy);
            if (man < minTargetDistanceTiles) continue;

            var d = world.GetOrGenerateTileData(g);
            if (d.kind != TileKind.Building) continue;

            if (!HasAdjacentRoad(g)) continue;
            return g;
        }

        return origin + new Vector2Int(minTargetDistanceTiles, 0);
    }

    private bool HasAdjacentRoad(Vector2Int building)
    {
        return world.GetOrGenerateTileData(building + new Vector2Int(1, 0)).kind == TileKind.Road
            || world.GetOrGenerateTileData(building + new Vector2Int(-1, 0)).kind == TileKind.Road
            || world.GetOrGenerateTileData(building + new Vector2Int(0, 1)).kind == TileKind.Road
            || world.GetOrGenerateTileData(building + new Vector2Int(0, -1)).kind == TileKind.Road;
    }

    private Vector3 ComputeSpawnPosition(Vector2Int building, Vector2Int dirToRoad)
    {
        float ts = world.TileSize;
        Vector3 center = GridToWorld(building);

        Vector3 dir = new Vector3(dirToRoad.x, 0f, dirToRoad.y).normalized;
        Vector3 perp = new Vector3(-dir.z, 0f, dir.x);

        float edgeHalf = ts * 0.5f;
        float along = Random.Range(-ts * alongEdgeRandom, ts * alongEdgeRandom);

        return center + dir * (edgeHalf + standOffFromBuilding) + perp * along;
    }

    private Vector3 GridToWorld(Vector2Int g) => new Vector3(g.x * world.TileSize, 0f, g.y * world.TileSize);

    private void Despawn(EdgeKey key)
    {
        if (!activeByEdge.TryGetValue(key, out var p)) return;
        activeByEdge.Remove(key);
        if (p != null) Destroy(p.gameObject);
    }

    private void OnTileUnloaded(Vector2Int grid, GameObject go)
    {
        edgeScratch.Clear();
        foreach (var kv in activeByEdge)
        {
            if (kv.Key.building == grid)
                edgeScratch.Add(kv.Key);
        }
        for (int i = 0; i < edgeScratch.Count; i++)
            Despawn(edgeScratch[i]);
    }

    private readonly struct EdgeKey
    {
        public readonly Vector2Int building;
        public readonly Vector2Int dir;
        public EdgeKey(Vector2Int b, Vector2Int d) { building = b; dir = d; }
    }
}
