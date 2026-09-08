using System;
using UnityEngine;

public static class CarGarage
{
    public static event Action Changed;
    private const string SelectionKey = "garage.selected";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Changed = null;
    public static bool IsOwned(CarCatalog.Entry car) => car.price == 0 || PlayerPrefs.GetInt("garage.owned." + car.id, 0) == 1;
    public static int SelectedIndex(CarCatalog catalog)
    {
        string id = PlayerPrefs.GetString(SelectionKey, "");
        for (int i = 0; i < catalog.cars.Length; i++)
            if (catalog.cars[i].id == id && IsOwned(catalog.cars[i])) return i;
        return 0;
    }
    public static bool SelectOrBuy(CarCatalog catalog, int index)
    {
        if (catalog == null || index < 0 || index >= catalog.cars.Length) return false;
        var car = catalog.cars[index];
        if (!IsOwned(car))
        {
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.TrySpend(car.price)) return false;
            PlayerPrefs.SetInt("garage.owned." + car.id, 1);
        }
        PlayerPrefs.SetString(SelectionKey, car.id);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }
}
