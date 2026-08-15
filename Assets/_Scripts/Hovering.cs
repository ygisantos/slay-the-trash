using UnityEngine;

public class Hovering : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverHeight = 0.5f;    // For 3D objects: world units, for UI: pixels
    public float hoverSpeed = 2f;       // Speed of hovering

    private Vector3 startPosition3D;
    private Vector2 startPositionUI;
    private RectTransform rectTransform;
    private bool isUI = false;

    void Start()
    {
        // Check if this is a UI element
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            isUI = true;
            startPositionUI = rectTransform.anchoredPosition;
        }
        else
        {
            startPosition3D = transform.position;
        }
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;

        if (isUI)
        {
            rectTransform.anchoredPosition = new Vector2(startPositionUI.x, startPositionUI.y + offset);
        }
        else
        {
            transform.position = new Vector3(transform.position.x, startPosition3D.y + offset, transform.position.z);
        }
    }
}
