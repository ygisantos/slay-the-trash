using UnityEngine;
using UnityEngine.InputSystem;

public class ArrowView : MonoBehaviour
{
    [SerializeField] private GameObject arrowHead;
    [SerializeField] private LineRenderer lineRenderer;
    private Vector3 startPosition;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Vector2 screenPos = GetCurrentScreenPosition();
        Vector3 endPosition = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        Vector3 direction = -(startPosition - arrowHead.transform.position).normalized;
        lineRenderer.SetPosition(1, endPosition - direction * 0.5f);
        arrowHead.transform.position = endPosition;
        arrowHead.transform.right = direction;
    }

    public void SetupArrow(Vector3 startPosition)
    {
        this.startPosition = startPosition;
        lineRenderer.SetPosition(0, startPosition);

        Vector2 screenPos = GetCurrentScreenPosition();
        Vector3 endPosition = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        lineRenderer.SetPosition(1, endPosition);
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