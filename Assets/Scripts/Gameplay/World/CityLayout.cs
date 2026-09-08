// Seeded square-tile city graphs, independent of streaming order and Unity's global random state.
public static class CityLayout
{
    public const int RegionSize = 32;
    private const int CellSize = 4;
    private const int RoomsPerSide = RegionSize / CellSize;
    private const int RegionCacheLimit = 256;
    private static readonly System.Collections.Generic.Dictionary<(int seed, int x, int y), bool[]> regions = new();
    private static readonly System.Collections.Generic.Queue<(int seed, int x, int y)> regionOrder = new();

    public static int PositiveMod(int value, int size) => (value % size + size) % size;
    public static int Region(int coordinate) => (coordinate - PositiveMod(coordinate, RegionSize)) / RegionSize;

    public static uint Hash(int seed, int x, int y)
    {
        unchecked
        {
            uint h = (uint)seed ^ (uint)x * 374761393u ^ (uint)y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    public static void ClearCache() { regions.Clear(); regionOrder.Clear(); CityLots.ClearCache(); }
    public static int CachedRegionCount => regions.Count;

    public static bool IsRoad(int seed, int x, int y)
    {
        var key = (seed, Region(x), Region(y));
        if (!regions.TryGetValue(key, out var roads))
        {
            roads = GenerateRegion(seed, key.Item2, key.Item3);
            if (regions.Count >= RegionCacheLimit) regions.Remove(regionOrder.Dequeue());
            regions.Add(key, roads); regionOrder.Enqueue(key);
        }
        return roads[PositiveMod(y, RegionSize) * RegionSize + PositiveMod(x, RegionSize)];
    }

    private static bool[] GenerateRegion(int seed, int rx, int ry)
    {
        var roads = new bool[RegionSize * RegionSize];
        int roomCount = RoomsPerSide * RoomsPerSide;
        var junctions = new (int x, int y)[roomCount + 4];
        for (int y = 0; y < RoomsPerSide; y++)
            for (int x = 0; x < RoomsPerSide; x++)
            {
                uint h = Hash(seed, rx * RoomsPerSide + x, ry * RoomsPerSide + y);
                junctions[y * RoomsPerSide + x] = (x * CellSize + 1 + (int)(h % 2), y * CellSize + 1 + (int)((h >> 8) % 2));
            }
        // The world starts at the city's origin junction, where the taxi is authored.
        if (rx == 0 && ry == 0) junctions[0] = (0, 0);

        // Boundary portals are vertices of the street graph from the outset.
        // Adjacent regions agree on their positions without consulting each other.
        int west = (int)(Hash(seed ^ 0x1723, rx, ry) % RoomsPerSide);
        int east = (int)(Hash(seed ^ 0x1723, rx + 1, ry) % RoomsPerSide);
        int south = (int)(Hash(seed ^ 0x7541, rx, ry) % RoomsPerSide);
        int north = (int)(Hash(seed ^ 0x7541, rx, ry + 1) % RoomsPerSide);
        int[] portalRooms = { west * RoomsPerSide, east * RoomsPerSide + RoomsPerSide - 1,
            south, (RoomsPerSide - 1) * RoomsPerSide + north };
        junctions[roomCount] = (0, west * CellSize + 1);
        junctions[roomCount + 1] = (RegionSize - 1, east * CellSize + 1);
        junctions[roomCount + 2] = (south * CellSize + 1, 0);
        junctions[roomCount + 3] = (north * CellSize + 1, RegionSize - 1);

        // A spatially weighted street graph, rather than a perfect maze. Edge
        // costs and neighborhood density are seeded independently. Kruskal growth
        // joins districts while retaining locally inexpensive alternate streets
        // in the same pass; no per-junction degree target or repair pass is used.
        var edges = new System.Collections.Generic.List<StreetEdge>();
        for (int y = 0; y < RoomsPerSide; y++)
            for (int x = 0; x < RoomsPerSide; x++)
            {
                int a = y * RoomsPerSide + x;
                if (x + 1 < RoomsPerSide) edges.Add(new StreetEdge(a, a + 1, Hash(seed ^ 0x3852, rx * RoomsPerSide + x, ry * RoomsPerSide + y)));
                if (y + 1 < RoomsPerSide) edges.Add(new StreetEdge(a, a + RoomsPerSide, Hash(seed ^ 0x6912, rx * RoomsPerSide + x, ry * RoomsPerSide + y)));
            }
        for (int p = 0; p < 4; p++) edges.Add(new StreetEdge(portalRooms[p], roomCount + p, 0));
        edges.Sort((a, b) => a.cost != b.cost ? a.cost.CompareTo(b.cost) : a.a != b.a ? a.a.CompareTo(b.a) : a.b.CompareTo(b.b));
        var parents = new int[junctions.Length];
        for (int i = 0; i < parents.Length; i++) parents[i] = i;
        foreach (var edge in edges)
        {
            int a = Find(parents, edge.a), b = Find(parents, edge.b);
            int gx = rx * RoomsPerSide + edge.a % RoomsPerSide, gy = ry * RoomsPerSide + edge.a / RoomsPerSide;
            int dx = (gx - PositiveMod(gx, 4)) / 4, dy = (gy - PositiveMod(gy, 4)) / 4;
            double density = .45 + .50 * System.Math.Sqrt(Hash(seed ^ 0x2859, dx, dy) / (double)uint.MaxValue);
            if (a == b && edge.cost / (double)uint.MaxValue > density) continue;
            parents[a] = b;
            bool horizontalFirst = (Hash(seed ^ 0x4591, edge.a + rx * 71, edge.b + ry * 71) & 1) == 0;
            if (edge.b >= roomCount) horizontalFirst = edge.b >= roomCount + 2;
            Carve(roads, junctions[edge.a], junctions[edge.b], horizontalFirst);
        }
        return roads;
    }

    private readonly struct StreetEdge
    {
        public readonly int a, b;
        public readonly uint cost;
        public StreetEdge(int a, int b, uint cost) { this.a = a; this.b = b; this.cost = cost; }
    }
    private static int Find(int[] parents, int node)
    {
        while (parents[node] != node) { parents[node] = parents[parents[node]]; node = parents[node]; }
        return node;
    }

    private static void Carve(bool[] roads, (int x, int y) from, (int x, int y) to, bool horizontalFirst)
    {
        roads[from.y * RegionSize + from.x] = true;
        while (from != to)
        {
            if ((horizontalFirst && from.x != to.x) || from.y == to.y) from.x += System.Math.Sign(to.x - from.x);
            else from.y += System.Math.Sign(to.y - from.y);
            roads[from.y * RegionSize + from.x] = true;
        }
    }

    public static bool IsPark(int seed, int x, int y) =>
        !IsRoad(seed, x, y) && !CityLots.TryGetLot(seed, x, y, out _);

    public static int BuildingVariant(int seed, int x, int y)
    {
        uint district = Hash(seed + 71, Region(x), Region(y));
        return (int)((district + Hash(seed, x, y) % 2) % 4);
    }
    public static (int x, int y) NearestRoad(int seed, int x, int y)
    {
        for (int radius = 0; radius <= CellSize * 2; radius++)
            for (int offset = -radius; offset <= radius; offset++)
            {
                int dy = radius - System.Math.Abs(offset);
                if (IsRoad(seed, x + offset, y + dy)) return (x + offset, y + dy);
                if (dy != 0 && IsRoad(seed, x + offset, y - dy)) return (x + offset, y - dy);
            }
        throw new System.InvalidOperationException("City layout contains no nearby connected road.");
    }

    public static int RoadDistance(int seed, int sx, int sy, int dx, int dy)
    {
        return FindRoadPath(seed, sx, sy, dx, dy).Count - 1;
    }

    // A* on the logical four-neighbor road graph. Every edge costs one tile.
    // Manhattan distance is admissible, so the returned path is shortest by road distance.
    public static System.Collections.Generic.List<(int x, int y)> FindRoadPath(int seed, int sx, int sy, int dx, int dy)
    {
        var path = new System.Collections.Generic.List<(int x, int y)>();
        if (!IsRoad(seed, sx, sy) || !IsRoad(seed, dx, dy)) return path;
        var start = (sx, sy); var goal = (dx, dy);
        var costs = new System.Collections.Generic.Dictionary<(int, int), int> { [start] = 0 };
        var parents = new System.Collections.Generic.Dictionary<(int, int), (int, int)>();
        var open = new RoadHeap();
        open.Push(new RoadNode(sx, sy, 0, System.Math.Abs(dx - sx) + System.Math.Abs(dy - sy)));
        while (open.Count > 0)
        {
            RoadNode current = open.Pop();
            var key = (current.x, current.y);
            if (costs[key] != current.cost) continue;
            if (key == goal)
            {
                path.Add(key);
                while (key != start) { key = parents[key]; path.Add(key); }
                path.Reverse();
                return path;
            }
            for (int direction = 0; direction < 4; direction++)
            {
                int x = current.x + (direction == 0 ? 1 : direction == 1 ? -1 : 0);
                int y = current.y + (direction == 2 ? 1 : direction == 3 ? -1 : 0);
                if (!IsRoad(seed, x, y)) continue;
                var next = (x, y); int cost = current.cost + 1;
                if (costs.TryGetValue(next, out int previous) && previous <= cost) continue;
                costs[next] = cost; parents[next] = key;
                open.Push(new RoadNode(x, y, cost, System.Math.Abs(dx - x) + System.Math.Abs(dy - y)));
            }
        }
        return path;
    }

    private readonly struct RoadNode
    {
        public readonly int x, y, cost, heuristic;
        public RoadNode(int x, int y, int cost, int heuristic)
        { this.x = x; this.y = y; this.cost = cost; this.heuristic = heuristic; }
    }

    private sealed class RoadHeap
    {
        private readonly System.Collections.Generic.List<RoadNode> nodes = new();
        public int Count => nodes.Count;
        private static bool Before(RoadNode a, RoadNode b)
        {
            int af = a.cost + a.heuristic, bf = b.cost + b.heuristic;
            if (af != bf) return af < bf;
            if (a.heuristic != b.heuristic) return a.heuristic < b.heuristic;
            return a.x != b.x ? a.x < b.x : a.y < b.y;
        }
        public void Push(RoadNode node)
        {
            int i = nodes.Count; nodes.Add(node);
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (!Before(node, nodes[parent])) break;
                nodes[i] = nodes[parent]; i = parent;
            }
            nodes[i] = node;
        }
        public RoadNode Pop()
        {
            RoadNode result = nodes[0], last = nodes[nodes.Count - 1];
            nodes.RemoveAt(nodes.Count - 1);
            if (nodes.Count == 0) return result;
            int i = 0;
            while (i * 2 + 1 < nodes.Count)
            {
                int child = i * 2 + 1;
                if (child + 1 < nodes.Count && Before(nodes[child + 1], nodes[child])) child++;
                if (!Before(nodes[child], last)) break;
                nodes[i] = nodes[child]; i = child;
            }
            nodes[i] = last;
            return result;
        }
    }
}
