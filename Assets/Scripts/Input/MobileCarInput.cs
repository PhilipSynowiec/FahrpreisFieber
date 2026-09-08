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

    public float Steer { get; private set; }
    public float Throttle { get; private set; } = 1f;
    private readonly HashSet<int> uiTouches = new HashSet<int>();
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private bool mouseStartedOnUI;

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
                    uiTouches.Remove(id);
                    continue;
                }
                hasTouch = true;
                Vector2 position = touch.position.ReadValue();
                if (touch.press.wasPressedThisFrame)
                {
                    uiTouches.Remove(id);
                    if (IsInteractiveUI(position)) uiTouches.Add(id);
                }
                if (uiTouches.Contains(id)) continue;
                AccumulatePress(position, ref left, ref right);
            }
        }

        var mouse = Mouse.current;
        if (!hasTouch && mouse != null)
        {
            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame) mouseStartedOnUI = IsInteractiveUI(position);
            if (mouse.leftButton.isPressed && !mouseStartedOnUI)
            {
                AccumulatePress(position, ref left, ref right);
            }
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

    private void AccumulatePress(Vector2 position, ref bool left, ref bool right)
    {
        // Areas are authored in the scene; they need no visible graphic or raycast target.
        if (Contains(leftArea, position)) left = true;
        else if (Contains(rightArea, position)) right = true;
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
        uiTouches.Clear();
        mouseStartedOnUI = false;
        Steer = 0f;
        Throttle = 1f;
    }
}
