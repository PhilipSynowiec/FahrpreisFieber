using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarStoreUI : MonoBehaviour
{
    [SerializeField] private CarCatalog catalog;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button[] purchaseButtons;
    [SerializeField] private TMP_Text[] actionLabels;
    private void Awake()
    {
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
        for (int i = 0; i < purchaseButtons.Length; i++)
        {
            int index = i;
            purchaseButtons[i].onClick.AddListener(() => { CarGarage.SelectOrBuy(catalog, index); Refresh(); });
        }
        panel.SetActive(false);
    }
    private void OnEnable() { CurrencyManager.BalanceChanged += OnBalance; CarGarage.Changed += Refresh; }
    private void OnDisable() { CurrencyManager.BalanceChanged -= OnBalance; CarGarage.Changed -= Refresh; }
    private void OnBalance(int _) => Refresh();
    public void Open() { panel.SetActive(true); Refresh(); }
    public void Close() => panel.SetActive(false);
    private void Refresh()
    {
        int selected = CarGarage.SelectedIndex(catalog);
        for (int i = 0; i < purchaseButtons.Length; i++)
        {
            var car = catalog.cars[i]; bool owned = CarGarage.IsOwned(car);
            actionLabels[i].text = i == selected ? "SELECTED" : owned ? "SELECT" : car.price.ToString("N0");
            purchaseButtons[i].interactable = i != selected && (owned || CurrencyManager.Balance >= car.price);
        }
    }
}
