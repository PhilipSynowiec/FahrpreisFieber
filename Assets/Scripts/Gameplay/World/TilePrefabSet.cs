using UnityEngine;

[CreateAssetMenu(menuName = "FahrpreisFieber/Tile Prefab Set")]
public class TilePrefabSet : ScriptableObject
{
    public GameObject roadPrefab;
    public GameObject buildingPrefab;

    // Optional later:
    // public GameObject[] roadVariants;
    // public GameObject[] buildingVariants;
}
