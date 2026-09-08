using UnityEngine;
using UnityEngine.UI;

public class DrivingViewMenu : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    private void Awake()
    {
        for (int i=0;i<buttons.Length;i++)
        {
            int index=i;
            buttons[i].onClick.AddListener(()=> { DrivingViewPreference.Set((DrivingView)index); Refresh(); });
        }
        Refresh();
    }
    private void Refresh()
    {
        for (int i=0;i<buttons.Length;i++) buttons[i].interactable=i!=(int)DrivingViewPreference.Selected;
    }
}
