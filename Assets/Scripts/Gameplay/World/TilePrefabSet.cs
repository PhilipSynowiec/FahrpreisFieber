using UnityEngine;

[CreateAssetMenu(menuName = "FahrpreisFieber/Tile Prefab Set")]
public class TilePrefabSet : ScriptableObject
{
    public GameObject roadPrefab;
    public GameObject buildingPrefab;

    public GameObject junctionPrefab;
    public GameObject parkPrefab;
    public GameObject[] buildingVariants;
}
