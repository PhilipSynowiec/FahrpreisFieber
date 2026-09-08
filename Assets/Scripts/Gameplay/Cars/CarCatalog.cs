using UnityEngine;

[CreateAssetMenu(menuName = "Taxi/Car Catalog")]
public class CarCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string id;
        public string displayName;
        public float topSpeed;
        public int price;
        public GameObject prefab;
        public Sprite thumbnail;
    }
    public Entry[] cars;
}
