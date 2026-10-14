using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public RectTransform container;    // holds the two tiled images
        public float tileWidth = 1920f;    // width of ONE image in UI units
        [Range(0f, 2f)]
        public float speedMultiplier = 1f; // 0 = static, 1 = normal, 2 = fast
    }

    [Header("Layers (index 0 = furthest back)")]
    [SerializeField] private ParallaxLayer[] layers;

    [Header("Scroll")]
    [SerializeField] private float baseSpeed = 100f; // pixels per second

    private float[] startX;

    void Start()
    {
        startX = new float[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].container != null)
                startX[i] = layers[i].container.anchoredPosition.x;
        }
    }

    void Update()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.container == null) continue;

            Vector2 pos = layer.container.anchoredPosition;
            pos.x -= baseSpeed * layer.speedMultiplier * Time.deltaTime;

            // Once we've scrolled exactly one tile width, snap back.
            // The second image is now exactly where the first started,
            // so the snap is seamless.
            if (pos.x >= startX[i] + layer.tileWidth)
                pos.x -= layer.tileWidth;

            layer.container.anchoredPosition = pos;
        }
    }
}