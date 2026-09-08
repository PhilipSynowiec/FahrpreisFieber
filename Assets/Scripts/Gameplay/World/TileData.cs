using UnityEngine;

public enum TileKind
{
    Road = 0,
    Building = 1,
    Park = 2
}

// Logical tile and its shared street-facing building parcel.
[System.Serializable]
public struct TileData
{
    public TileKind kind;
    public int variantId;
    public Vector2Int lotOrigin;

    public static TileData Park(int variantId = 0) => new TileData { kind = TileKind.Park, variantId = variantId };

    public static TileData Road(int variantId = 0) => new TileData { kind = TileKind.Road, variantId = variantId };
    public static TileData Building(int variantId, CityLots.Lot lot) => new TileData { kind = TileKind.Building, variantId = variantId,
        lotOrigin = new Vector2Int(lot.AnchorX, lot.AnchorY) };
}
