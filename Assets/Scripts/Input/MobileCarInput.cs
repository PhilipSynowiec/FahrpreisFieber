using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Sample before physics so releasing a finger cannot leave stale steering behind.
[DefaultExecutionOrder(-100)]
public class MobileCarInput : MonoBehaviour
{
    [Header("Screen hold areas (scene Canvas)")]
    [SerializeField] private RectTransform leftArea;
    [SerializeField] private RectTransform rightArea;
    [Tooltip("Show the areas' tint and arrow Images outside mobile devices, e.g. for testing in the editor.")]
    [SerializeField] private bool showAreasOnDesktop;

    public float Steer { get; private set; }
    public float Throttle { get; private set; } = 1f;
    // Each press keeps the side it started on, so a sliding thumb does not drop steering.
    private enum Side { None, Left, Right }
    private readonly Dictionary<int, Side> touchSides = new Dictionary<int, Side>();
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private Side mouseSide;

    private void Awake()
    {
        // The tint is only a touch hint; desktop players steer with the keyboard.
        bool show = Application.isMobilePlatform || showAreasOnDesktop;
        SetAreaVisible(leftArea, show);
        SetAreaVisible(rightArea, show);
    }

    private static void SetAreaVisible(RectTransform area, bool visible)
    {
        if (area == null) return;
        foreach (var image in area.GetComponentsInChildren<Image>(true)) image.enabled = visible;
    }

    private void Update()
    {
        bool left = false, right = false;
        var screen = Touchscreen.current;
        bool hasTouch = false;
        if (screen != null)
        {
            foreach (var touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                if (!touch.press.isPressed)
                {
                    touchSides.Remove(id);
                    continue;
                }
                hasTouch = true;
                // A touch already held when this component enabled is classified on first sight.
                if (touch.press.wasPressedThisFrame || !touchSides.TryGetValue(id, out Side side))
                {
                    side = SideAt(touch.position.ReadValue());
                    touchSides[id] = side;
                }
                Accumulate(side, ref left, ref right);
            }
        }

        var mouse = Mouse.current;
        if (!hasTouch && mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame) mouseSide = SideAt(mouse.position.ReadValue());
            if (mouse.leftButton.isPressed) Accumulate(mouseSide, ref left, ref right);
        }

        // Keyboard equivalents also make two-sided braking easy to test in the editor.
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            left |= keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            right |= keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.spaceKey.isPressed)
                left = right = true;
        }
        Steer = left == right ? 0f : (left ? -1f : 1f);
        Throttle = left && right ? -1f : 1f;
    }

    private Side SideAt(Vector2 position)
    {
        if (IsInteractiveUI(position)) return Side.None;
        // Areas are authored in the scene; their tint and arrow Images must not be raycast targets.
        if (Contains(leftArea, position)) return Side.Left;
        if (Contains(rightArea, position)) return Side.Right;
        return Side.None;
    }

    private static void Accumulate(Side side, ref bool left, ref bool right)
    {
        if (side == Side.Left) left = true;
        else if (side == Side.Right) right = true;
    }

    private static bool Contains(RectTransform area, Vector2 position)
    {
        if (area == null || !area.gameObject.activeInHierarchy) return false;
        var canvas = area.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(area, position, camera);
    }

    private bool IsInteractiveUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        hits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        foreach (var hit in hits)
            if (hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
        return false;
    }

    private void OnDisable() => ResetInput();
    private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
    private void ResetInput()
    {
        touchSides.Clear();
        mouseSide = Side.None;
        Steer = 0f;
        Throttle = 1f;
    }
}
