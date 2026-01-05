using UnityEngine;

public enum TileKind
{
    Road = 0,
    Building = 1
}

/// <summary>
/// Lightweight logical data for one grid cell.
/// Extend this with whatever you need (variant, flags, connections, etc.).
/// </summary>
[System.Serializable]
public struct TileData
{
    public TileKind kind;
    public int variantId;

    public static TileData Road(int variantId = 0) => new TileData { kind = TileKind.Road, variantId = variantId };
    public static TileData Building(int variantId = 0) => new TileData { kind = TileKind.Building, variantId = variantId };
}
