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

    private Vector2Int lastCenter = new(int.MinValue, int.MinValue);
    private int radiusSqr;

    public float TileSize => tileSize;

    public event Action<Vector2Int, GameObject, TileData> TileSpawned;
    public event Action<Vector2Int, GameObject> TileUnloaded;

    private void Awake()
    {
        radiusSqr = radiusTiles * radiusTiles;
    }

    private void Start()
    {
        Refresh(force: true);
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
        if (!force && center == lastCenter) return;
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

        foreach (var g in shouldExist)
        {
            if (!spawned.ContainsKey(g))
                buildQueue.Enqueue(g);
        }

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

        GameObject prefab = (data.kind == TileKind.Road) ? prefabs.roadPrefab : prefabs.buildingPrefab;
        if (prefab == null) return;

        Vector3 pos = GridToWorld(g);
        if (data.kind == TileKind.Building) pos.y = 0.5f;
        var go = Instantiate(prefab, pos, Quaternion.identity, transform);
        go.name = $"Tile_{g.x}_{g.y}_{data.kind}";

        var inst = go.GetComponent<TileInstance>();
        if (inst == null) inst = go.AddComponent<TileInstance>();
        inst.Init(g, data);

        spawned[g] = go;
        TileSpawned?.Invoke(g, go, data);
    }

    private void UnloadTile(Vector2Int g)
    {
        if (!spawned.TryGetValue(g, out var go)) return;
        spawned.Remove(g);

        TileUnloaded?.Invoke(g, go);
        if (go != null) Destroy(go);
    }

    public bool IsSpawned(Vector2Int g) => spawned.ContainsKey(g);

    public bool TryGetTileData(Vector2Int g, out TileData data) => tileDataCache.TryGetValue(g, out data);

    public TileData GetOrGenerateTileData(Vector2Int g)
    {
        if (!tileDataCache.TryGetValue(g, out var data))
        {
            data = GenerateTile(g.x, g.y);
            tileDataCache[g] = data;
        }
        return data;
    }

    public IEnumerable<KeyValuePair<Vector2Int, GameObject>> EnumerateSpawnedTiles() => spawned;

    private Vector2Int WorldToGrid(Vector3 world)
    {
        int gx = Mathf.FloorToInt(world.x / tileSize);
        int gy = Mathf.FloorToInt(world.z / tileSize);
        return new Vector2Int(gx, gy);
    }

    private Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(g.x * tileSize, 0f, g.y * tileSize);
    }

    // =========================
    // YOU replace this with your logic
    // =========================
    private TileData GenerateTile(int gx, int gy)
    {
        int h = Hash(worldSeed, gx, gy);
        bool mainRoad = (gx % 8 == 0) || (gy % 8 == 0);
        bool extraRoad = ((h & 31) == 0);
        bool isRoad = mainRoad || extraRoad;
        int variant = (h >> 8) & 3;
        return isRoad ? TileData.Road(variant) : TileData.Building(variant);
    }

    private static int Hash(int seed, int x, int y)
    {
        unchecked
        {
            int h = seed;
            h = h * 31 + x;
            h = h * 31 + y;
            h ^= (h << 13);
            h ^= (h >> 17);
            h ^= (h << 5);
            return h;
        }
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
