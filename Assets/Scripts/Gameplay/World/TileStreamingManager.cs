using System;
using System.Collections.Generic;
using UnityEngine;

public class TileStreamingManager : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Grid")]
    [SerializeField] private float tileSize = 2f;
    [SerializeField] private int radiusTiles = 12;
    [SerializeField] private bool circularRadius = true;

    [Header("Prefabs")]
    [SerializeField] private TilePrefabSet prefabs;

    [Header("Streaming")]
    [SerializeField] private int buildPerFrame = 30;
    [SerializeField] private bool updateOnlyOnTileChange = true;

    [Header("Determinism")]
    [SerializeField] private int worldSeed = 12345;

    // Logical world data
    private readonly Dictionary<Vector2Int, TileData> tileDataCache = new();
    // Rendered instances only
    private readonly Dictionary<Vector2Int, GameObject> spawned = new();
    // Build queue
    private readonly Queue<Vector2Int> buildQueue = new();
    private readonly Dictionary<Vector2Int, (GameObject visual, int users)> buildingLots = new();

    private Vector2Int lastCenter = new(int.MinValue, int.MinValue);
    private int radiusSqr;

    public float TileSize => tileSize;
    public int WorldSeed => worldSeed;

    public event Action<Vector2Int, GameObject> TileUnloaded;

    private void Awake()
    {
        radiusSqr = radiusTiles * radiusTiles;
    }

    private void Start()
    {
        Refresh(force: true);
        BuildSomeTiles();
    }

    private void Update()
    {
        if (target == null || prefabs == null) return;

        if (updateOnlyOnTileChange)
        {
            var c = WorldToGrid(target.position);
            if (c != lastCenter) Refresh(force: false);
        }
        else
        {
            Refresh(force: false);
        }

        BuildSomeTiles();
    }

    public void Refresh(bool force)
    {
        if (target == null || prefabs == null) return;

        Vector2Int center = WorldToGrid(target.position);
        if (!force && updateOnlyOnTileChange && center == lastCenter) return;
        radiusSqr = radiusTiles * radiusTiles;
        lastCenter = center;

        var shouldExist = HashSetPool<Vector2Int>.Get();

        for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
        {
            for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
            {
                if (circularRadius && (dx * dx + dy * dy) > radiusSqr) continue;
                shouldExist.Add(new Vector2Int(center.x + dx, center.y + dy));
            }
        }

        // Rebuild only the current demand; never accumulate duplicate or out-of-range work.
        buildQueue.Clear();
        var pending = ListPool<Vector2Int>.Get();
        foreach (var g in shouldExist)
            if (!spawned.ContainsKey(g)) pending.Add(g);
        pending.Sort((a, b) => (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));
        foreach (var g in pending) buildQueue.Enqueue(g);
        ListPool<Vector2Int>.Release(pending);

        var toRemove = ListPool<Vector2Int>.Get();
        foreach (var kv in spawned)
        {
            if (!shouldExist.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }
        for (int i = 0; i < toRemove.Count; i++)
            UnloadTile(toRemove[i]);

        ListPool<Vector2Int>.Release(toRemove);
        HashSetPool<Vector2Int>.Release(shouldExist);
    }

    private void BuildSomeTiles()
    {
        int n = Mathf.Max(1, buildPerFrame);
        while (n-- > 0 && buildQueue.Count > 0)
        {
            var g = buildQueue.Dequeue();
            if (spawned.ContainsKey(g)) continue;
            EnsureTile(g);
        }
    }

    private void EnsureTile(Vector2Int g)
    {
        TileData data = GetOrGenerateTileData(g);

        GameObject prefab = prefabs.roadPrefab;
        Quaternion rotation = Quaternion.identity;
        if (data.kind == TileKind.Building)
        {
            AcquireBuilding(data);
            var tile = new GameObject($"Tile_{g.x}_{g.y}_Building");
            tile.transform.SetParent(transform, false); tile.transform.position = GridToWorld(g);
            tile.AddComponent<TileInstance>().Init(g, data);
            tileDataCache[g] = data; spawned[g] = tile;
            return;
        }
        if (data.kind == TileKind.Park) prefab = prefabs.parkPrefab;
        else
        {
            bool left = CityLayout.IsRoad(worldSeed, g.x - 1, g.y);
            bool right = CityLayout.IsRoad(worldSeed, g.x + 1, g.y);
            bool down = CityLayout.IsRoad(worldSeed, g.x, g.y - 1);
            bool up = CityLayout.IsRoad(worldSeed, g.x, g.y + 1);
            bool horizontal = left && right && !up && !down;
            bool vertical = up && down && !left && !right;
            if (!horizontal && !vertical && prefabs.junctionPrefab != null) prefab = prefabs.junctionPrefab;
            else if (horizontal) rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        if (prefab == null) return;

        var go = Instantiate(prefab, GridToWorld(g), rotation, transform);
        // Authored city assets use a ten-meter tile footprint.
        go.transform.localScale *= tileSize / 10f;
        go.name = $"Tile_{g.x}_{g.y}_{data.kind}";

        var inst = go.GetComponent<TileInstance>();
        if (inst == null) inst = go.AddComponent<TileInstance>();
        inst.Init(g, data);

        tileDataCache[g] = data;
        spawned[g] = go;
    }

    private void UnloadTile(Vector2Int g)
    {
        if (!spawned.TryGetValue(g, out var go)) return;
        spawned.Remove(g);
        if (tileDataCache.TryGetValue(g, out var data) && data.kind == TileKind.Building) ReleaseBuilding(data.lotOrigin);
        tileDataCache.Remove(g);

        TileUnloaded?.Invoke(g, go);
        if (go != null) Destroy(go);
    }

    public int GetRoadDistance(Vector2Int from, Vector2Int to) => CityLayout.RoadDistance(worldSeed, from.x, from.y, to.x, to.y);

    public bool IsSpawned(Vector2Int g) => spawned.ContainsKey(g);


    public TileData GetOrGenerateTileData(Vector2Int g)
    {
        if (!tileDataCache.TryGetValue(g, out var data))
        {
            data = GenerateTile(g.x, g.y);
            // Unloaded job lookups must not grow an unbounded world cache.
        }
        return data;
    }

    public IEnumerable<KeyValuePair<Vector2Int, GameObject>> EnumerateSpawnedTiles() => spawned;

    public Vector2Int WorldToGrid(Vector3 world)
    {
        int gx = Mathf.FloorToInt(world.x / tileSize + 0.5f);
        int gy = Mathf.FloorToInt(world.z / tileSize + 0.5f);
        return new Vector2Int(gx, gy);
    }

    private Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(g.x * tileSize, 0f, g.y * tileSize);
    }

    private TileData GenerateTile(int gx, int gy)
    {
        if (CityLayout.IsRoad(worldSeed, gx, gy)) return TileData.Road();
        if (!CityLots.TryGetLot(worldSeed, gx, gy, out var lot)) return TileData.Park();
        return TileData.Building(CityLayout.BuildingVariant(worldSeed, lot.x, lot.y), lot);
    }

    private void AcquireBuilding(TileData data)
    {
        if (buildingLots.TryGetValue(data.lotOrigin, out var existing))
        {
            buildingLots[data.lotOrigin] = (existing.visual, existing.users + 1);
            return;
        }
        var variants = prefabs.buildingVariants;
        var prefab = variants != null && variants.Length > 0 ? variants[data.variantId % variants.Length] : prefabs.buildingPrefab;
        if (!CityLots.TryGetLot(worldSeed, data.lotOrigin.x, data.lotOrigin.y, out var lot))
            throw new InvalidOperationException("Building parcel anchor is missing.");
        var visual = new GameObject($"BuildingLot_{lot.AnchorX}_{lot.AnchorY}");
        visual.transform.SetParent(transform, false);
        visual.transform.position = new Vector3(lot.x*tileSize, 0, lot.y*tileSize);
        visual.AddComponent<BuildingLotVisual>().Build(prefab, lot, tileSize);
        buildingLots.Add(data.lotOrigin, (visual, 1));
    }

    private void ReleaseBuilding(Vector2Int origin)
    {
        if (!buildingLots.TryGetValue(origin, out var lot)) return;
        if (lot.users > 1) buildingLots[origin] = (lot.visual, lot.users-1);
        else { buildingLots.Remove(origin); if (lot.visual != null) Destroy(lot.visual); }
    }

    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new();
        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(256);
        public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
    }

    private static class HashSetPool<T>
    {
        private static readonly Stack<HashSet<T>> pool = new();
        public static HashSet<T> Get() => pool.Count > 0 ? pool.Pop() : new HashSet<T>();
        public static void Release(HashSet<T> set) { set.Clear(); pool.Push(set); }
    }
}
