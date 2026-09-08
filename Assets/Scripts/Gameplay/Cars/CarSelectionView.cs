using UnityEngine;

// All six model instances are authored in the scene, including the starter car.
public class CarSelectionView : MonoBehaviour
{
    [SerializeField] private CarCatalog catalog;
    [SerializeField] private GameObject[] models;
    [SerializeField] private CarControllerInputSystem controller;
    private void OnEnable() { CarGarage.Changed += Refresh; Refresh(); }
    private void OnDisable() => CarGarage.Changed -= Refresh;
    public void Refresh()
    {
        if (catalog == null || catalog.cars == null || catalog.cars.Length == 0) return;
        int selected = CarGarage.SelectedIndex(catalog);
        for (int i = 0; i < models.Length; i++) if (models[i] != null) models[i].SetActive(i == selected);
        if (controller != null) controller.SetTopSpeed(catalog.cars[selected].topSpeed);
    }
}
