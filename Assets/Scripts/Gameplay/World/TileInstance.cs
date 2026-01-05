using UnityEngine;

public class TileInstance : MonoBehaviour
{
    public Vector2Int GridPos { get; private set; }
    public TileData Data { get; private set; }

    public void Init(Vector2Int gridPos, TileData data)
    {
        GridPos = gridPos;
        Data = data;
    }
}
