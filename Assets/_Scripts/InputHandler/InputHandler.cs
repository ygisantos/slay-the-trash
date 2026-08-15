using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance;
    private Camera cam;
    private CardView draggingCard;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    public void OnPress(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        Vector2 screenPos = GetCurrentScreenPosition();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CardView cv = hit.collider.GetComponent<CardView>();
            if (cv != null)
            {
                draggingCard = cv;
                cv.StartDrag(screenPos);
            }
        }
    }

    public void OnDrag(InputAction.CallbackContext ctx)
    {
        if (draggingCard == null) return;

        Vector2 screenPos = GetCurrentScreenPosition();
        draggingCard.DragTo(screenPos);
    }

    public void OnRelease(InputAction.CallbackContext ctx)
    {
        if (!ctx.canceled || draggingCard == null) return;

        Vector2 screenPos = GetCurrentScreenPosition();
        draggingCard.EndDrag(screenPos);
        draggingCard = null;
    }

    private Vector2 GetCurrentScreenPosition()
    {
        // Check if using touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }
        // Otherwise use mouse
        else if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        // Fallback to center of screen
        return new Vector2(Screen.width / 2f, Screen.height / 2f);
    }
}