using UnityEngine;
using UnityEngine.InputSystem;

public class ManualTargetSystem : Singleton<ManualTargetSystem>
{
    [SerializeField] private ArrowView arrowView;
    [SerializeField] private LayerMask targetLayerMask;
    private Camera cam;

    protected override void Awake()
    {
        base.Awake();
        cam = Camera.main;
    }

    public void StartTargeting(Vector3 startPosition)
    {
        arrowView.gameObject.SetActive(true);
        arrowView.SetupArrow(startPosition);
    }

    public EnemyView EndTargeting(Vector2 screenPos)
    {
        arrowView.gameObject.SetActive(false);
        
        if (cam == null)
        {
            return null;
        }
        
        Ray ray = cam.ScreenPointToRay(screenPos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, targetLayerMask))
        {
            if (hit.transform.TryGetComponent(out EnemyView enemyView))
            {
                return enemyView;
            }
        }
        
        return null;
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