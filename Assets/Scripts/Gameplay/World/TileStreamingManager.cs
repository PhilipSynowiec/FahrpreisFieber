using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Streams individual tiles around a target (car). No chunks.
/// Keeps logical data and spawned visuals separate.
/// - tileDataCache: stores TileData for already generated tiles (can be bounded later)
/// - spawned: stores instantiated GameObjects for currently visible tiles
///
/// You implement GenerateTile(gx, gy) to define the city logic.
/// </summary>
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
    [Tooltip("How many tiles to instantiate per frame (prevents spikes).")]
    [SerializeField] private int buildPerFrame = 30;

    [Tooltip("If true, recompute visibility only when the target enters a new tile.")]
    [SerializeField] private bool updateOnlyOnTileChange = true;

    [Header("Determinism")]
    [SerializeField] private int worldSeed = 12345;

    // Logical world data (may grow if you drive forever; you can bound this later)
    private readonly Dictionary<Vector2Int, TileData> tileDataCache = new();

    // Rendered instances only (bounded by radius)
    private readonly Dictionary<Vector2Int, GameObject> spawned = new();

    // Queue for building to avoid frame spikes
    private readonly Queue<Vector2Int> buildQueue = new();

    private Vector2Int lastCenter = new(int.MinValue, int.MinValue);
    private int radiusSqr;

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

        // Determine which tiles should be visible
        var shouldExist = HashSetPool<Vector2Int>.Get();

        for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
        {
            for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
            {
                if (circularRadius && (dx * dx + dy * dy) > radiusSqr) continue;
                shouldExist.Add(new Vector2Int(center.x + dx, center.y + dy));
            }
        }

        // Enqueue missing tiles for building
        foreach (var g in shouldExist)
        {
            if (!spawned.ContainsKey(g))
                buildQueue.Enqueue(g);
        }

        // Unload tiles not in the visible set
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
            if (spawned.ContainsKey(g)) continue; // may have been built already

            EnsureTile(g);
        }
    }

    private void EnsureTile(Vector2Int g)
    {
        // Fetch or generate logical data
        if (!tileDataCache.TryGetValue(g, out var data))
        {
            data = GenerateTile(g.x, g.y);
            tileDataCache[g] = data;
        }

        // Choose prefab
        GameObject prefab = (data.kind == TileKind.Road) ? prefabs.roadPrefab : prefabs.buildingPrefab;
        if (prefab == null) return;

        // Spawn visual instance
        Vector3 pos = GridToWorld(g);
        var go = Instantiate(prefab, pos, Quaternion.identity, transform);
        go.name = $"Tile_{g.x}_{g.y}_{data.kind}";
        spawned[g] = go;
    }

    private void UnloadTile(Vector2Int g)
    {
        if (!spawned.TryGetValue(g, out var go)) return;
        spawned.Remove(g);
        if (go != null) Destroy(go);
    }

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
    // YOU IMPLEMENT THIS METHOD
    // =========================
    private TileData GenerateTile(int gx, int gy)
    {
        // Replace this stub with your coherent city logic.
        // REQUIREMENT: deterministic for the same (worldSeed, gx, gy)
        // so tiles don't change when they unload/reload.

        int h = Hash(worldSeed, gx, gy);

        // Very simple placeholder:
        // - main grid roads every 8 tiles
        // - otherwise mostly buildings with occasional roads
        bool mainRoad = (gx % 8 == 0) || (gy % 8 == 0);
        bool extraRoad = ((h & 31) == 0); // ~3% roads
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

    // Small pools to reduce GC allocations
    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new();

        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(256);

        public static void Release(List<T> list)
        {
            list.Clear();
            pool.Push(list);
        }
    }

    private static class HashSetPool<T>
    {
        private static readonly Stack<HashSet<T>> pool = new();

        public static HashSet<T> Get() => pool.Count > 0 ? pool.Pop() : new HashSet<T>();

        public static void Release(HashSet<T> set)
        {
            set.Clear();
            pool.Push(set);
        }
    }
}
