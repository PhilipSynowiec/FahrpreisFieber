using UnityEngine;

[System.Serializable]
public class PassengerJob
{
    public Vector2Int originBuilding;
    public Vector2Int destinationBuilding;
    public Vector3 destinationWorld; // Reachable curbside drop-off, shared by marker and arrow.
    public float timeLimitSeconds;

    public float distanceWorld;          // road-route distance (for price + rating)
    public int quotedPriceCoins;         // computed at offer time
}
