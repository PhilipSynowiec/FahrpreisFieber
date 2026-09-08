using UnityEngine;
using UnityEngine.UI;

// Logical city tiles and shortest-road routes, clipped analytically by the UI material.
[ExecuteAlways]
public class TaxiMinimapGraphic : MaskableGraphic
{
    [SerializeField] private TileStreamingManager world;
    [SerializeField] private Transform car;
    [SerializeField] private TripManager trips;
    [SerializeField, Min(2)] private int radiusTiles = 6;
    private float nextRefresh;
    [SerializeField] private TaxiRouteNavigation navigation;
    private Vector2 mapCenter;
    private float mapRadius;

    private void Update()
    {
        if (!Application.isPlaying || Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.1f;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (world == null || car == null || world.TileSize <= 0f) return;
        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        mapCenter = rect.center; mapRadius = Mathf.Max(1f, radius);
        float scale = radius / (Mathf.Max(2, radiusTiles) * world.TileSize);
        int gx = Mathf.FloorToInt(car.position.x / world.TileSize + 0.5f);
        int gy = Mathf.FloorToInt(car.position.z / world.TileSize + 0.5f);
        float halfTile = world.TileSize * scale * 0.5f;
        for (int x = gx - radiusTiles - 1; x <= gx + radiusTiles + 1; x++)
            for (int y = gy - radiusTiles - 1; y <= gy + radiusTiles + 1; y++)
            {
                TileData tile = world.GetOrGenerateTileData(new Vector2Int(x, y));
                Color32 color = tile.kind == TileKind.Road ? new Color32(175, 190, 193, 255)
                    : tile.kind == TileKind.Park ? new Color32(67, 116, 84, 255) : new Color32(40, 58, 75, 255);
                Vector2 center = rect.center + new Vector2(x * world.TileSize - car.position.x, y * world.TileSize - car.position.z) * scale;
                Quad(vh, center, halfTile, color);
            }

        if (navigation != null)
        {
            navigation.RefreshRoute();
            var path = navigation.Route;
            for (int pass = 0; pass < 2; pass++)
                for (int n = 1; n < path.Count; n++)
                {
                    Vector2 a = rect.center + (new Vector2(path[n-1].x, path[n-1].y) * world.TileSize - new Vector2(car.position.x, car.position.z)) * scale;
                    Vector2 b = rect.center + (new Vector2(path[n].x, path[n].y) * world.TileSize - new Vector2(car.position.x, car.position.z)) * scale;
                    Vector2 routeSide = new Vector2(-(b-a).y, (b-a).x).normalized * (pass == 0 ? 3f : 1.6f);
                    Color32 ink = pass == 0 ? new Color32(22,32,43,255) : new Color32(255,196,41,255);
                    Triangle(vh,a-routeSide,a+routeSide,b+routeSide,ink);
                    Triangle(vh,a-routeSide,b+routeSide,b-routeSide,ink);
                    Quad(vh,b,pass == 0 ? 3f : 1.6f,ink);
                }
        }
        if (trips != null && trips.HasActiveTrip)
        {
            Vector3 offset = trips.ActiveJob.destinationWorld - car.position;
            // Keep the true bearing; destinations beyond the visible area sit on its rim.
            Vector2 destination = Vector2.ClampMagnitude(new Vector2(offset.x, offset.z) * scale, radius);
            Quad(vh, rect.center + destination, 6f, new Color32(0, 0, 0, 255), false);
            Quad(vh, rect.center + destination, 5f, new Color32(255, 196, 41, 255), false);
        }
        // North stays up; the taxi triangle rotates with its real heading.
        Vector2 forward = new Vector2(car.forward.x, car.forward.z).normalized;
        Vector2 side = new Vector2(forward.y, -forward.x);
        Triangle(vh, rect.center + forward * 9f, rect.center - forward * 6f - side * 6f,
            rect.center - forward * 6f + side * 6f, new Color32(255, 196, 41, 255));
    }

    private void Vertex(VertexHelper vh, Vector2 p, Color32 color, bool clipToCircle = true) =>
        vh.AddVert(p, color, clipToCircle ? (p - mapCenter) / mapRadius : Vector2.zero);

    private void Quad(VertexHelper vh, Vector2 center, float half, Color32 color, bool clipToCircle = true)
    {
        int i = vh.currentVertCount;
        // The destination pin straddles the rim, so keep the entire pin visible.
        Vertex(vh, center + new Vector2(-half, -half), color, clipToCircle);
        Vertex(vh, center + new Vector2(-half, half), color, clipToCircle);
        Vertex(vh, center + new Vector2(half, half), color, clipToCircle);
        Vertex(vh, center + new Vector2(half, -half), color, clipToCircle);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }

    private void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int i = vh.currentVertCount;
        Vertex(vh, a, color); Vertex(vh, b, color); Vertex(vh, c, color);
        vh.AddTriangle(i, i + 1, i + 2);
    }
}
