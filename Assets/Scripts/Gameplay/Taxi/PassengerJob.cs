using UnityEngine;

[System.Serializable]
public class PassengerJob
{
    public Vector2Int originBuilding;
    public Vector2Int destinationBuilding;
    public float timeLimitSeconds;

    public float distanceWorld;          // straight-line approximation (for price + rating)
    public float requiredAvgSpeed;       // distance / timeLimit
    public int quotedPriceCoins;         // computed at offer time
}
