using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Full-screen burn / heat-wave effect inspired by Pokémon status affliction ticks.
// Created dynamically at runtime with zero Unity Editor setup required.
public class ScreenBurnOverlay : MonoBehaviour
{
    private static ScreenBurnOverlay instance;
    private CanvasGroup canvasGroup;
    private Image overlayImage;
    private Sequence sequence;
    private static Sprite burnVignetteSprite;

    public static void Show(Color? tint = null)
    {
        if (instance == null)
            instance = Create();

        instance.Play(tint ?? new Color(1f, 0.38f, 0.08f, 0.85f));
    }

    private static ScreenBurnOverlay Create()
    {
        GameObject root = new GameObject("ScreenBurnOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450; // Below TurnBanner (500), above standard game UI

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        ScreenBurnOverlay overlay = root.AddComponent<ScreenBurnOverlay>();
        overlay.Build();
        return overlay;
    }

    private void Build()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject imageObj = new GameObject("BurnImage", typeof(RectTransform), typeof(Image));
        imageObj.transform.SetParent(transform, false);

        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlayImage = imageObj.GetComponent<Image>();
        overlayImage.raycastTarget = false;
        overlayImage.sprite = GetBurnVignetteSprite();
        overlayImage.type = Image.Type.Simple;
    }

    public void Play(Color tint)
    {
        sequence?.Kill();
        overlayImage.color = tint;

        // Sequence: Fast fiery burst -> heat wave flicker -> smooth dissipation
        sequence = DOTween.Sequence().SetUpdate(true);
        canvasGroup.alpha = 0f;

        // 1. Initial burst flash
        sequence.Append(
            DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0.9f, 0.08f)
                .SetEase(Ease.OutQuad)
        );

        // 2. Heat wave pulse / flicker
        sequence.Append(
            DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0.55f, 0.07f)
                .SetEase(Ease.InOutSine)
        );
        sequence.Append(
            DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0.82f, 0.07f)
                .SetEase(Ease.OutQuad)
        );

        // 3. Dissipate / fade out smoothly
        sequence.Append(
            DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0f, 0.28f)
                .SetEase(Ease.InCubic)
        );

        sequence.OnComplete(() =>
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        });
    }

    // Procedurally generated flame-vignette texture with soft center and fiery licking border
    private static Sprite GetBurnVignetteSprite()
    {
        if (burnVignetteSprite != null)
            return burnVignetteSprite;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Wavy flame modulation along perimeter
                float angle = Mathf.Atan2(dy, dx);
                float wave = Mathf.Sin(angle * 8f) * 0.05f + Mathf.Cos(angle * 14f) * 0.03f;
                float modulatedDist = dist + wave;

                // Center is translucent, outer edges are thick fiery vignette
                float alpha = Mathf.Clamp01((modulatedDist - 0.45f) / 0.65f);
                alpha = Mathf.Pow(alpha, 1.35f);

                // Add subtle warm wash across center
                float centerHeat = Mathf.Clamp01(1f - dist) * 0.18f;
                float finalAlpha = Mathf.Clamp01(alpha + centerHeat);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
            }
        }

        texture.Apply();
        burnVignetteSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return burnVignetteSprite;
    }
}

