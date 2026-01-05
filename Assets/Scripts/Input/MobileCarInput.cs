using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileCarInput : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider steerSlider;
    [SerializeField] private Button accelButton;
    [SerializeField] private Button brakeButton;

    public float Steer { get; private set; }
    public float Throttle { get; private set; }

    private bool accelPressed;
    private bool brakePressed;

    private void Awake()
    {
        // Add event triggers for pointer down/up
        AddPointerEvents(accelButton, () => accelPressed = true, () => accelPressed = false);
        AddPointerEvents(brakeButton, () => brakePressed = true, () => brakePressed = false);
    }

    private void Update()
    {
        Steer = steerSlider != null ? steerSlider.value : 0f;

        Throttle = 0f;
        if (accelPressed)
            Throttle = 1f;
        else if (brakePressed)
            Throttle = -1f;
    }

    private void AddPointerEvents(Button button, System.Action onDown, System.Action onUp)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        // Pointer Down
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { onDown?.Invoke(); });
        trigger.triggers.Add(entryDown);

        // Pointer Up
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { onUp?.Invoke(); });
        trigger.triggers.Add(entryUp);
    }
}
