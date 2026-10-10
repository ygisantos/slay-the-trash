using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Full-width banner that slides in to announce whose turn it is. Built in code.
public class TurnBanner : MonoBehaviour
{
    public static readonly Color PlayerColor = new Color(0.55f, 1f, 0.55f, 1f);
    public static readonly Color EnemyColor = new Color(1f, 0.4f, 0.4f, 1f);

    private static TurnBanner instance;

    private CanvasGroup group;
    private RectTransform bar;
    private RectTransform textRect;
    private TextMeshProUGUI text;
    private Image barImage;
    private Sequence sequence;

    public static void Show(string message, Color color)
    {
        if (instance == null)
            instance = Create();

        instance.Play(message, color);
    }

    // Call this when entering win/lose screens to ensure the banner never overlaps them.
    public static void Hide()
    {
        if (instance == null)
            return;

        instance.sequence?.Kill();
        instance.group.alpha = 0f;
    }

    private static TurnBanner Create()
    {
        GameObject root = new GameObject("TurnBanner", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        TurnBanner banner = root.AddComponent<TurnBanner>();
        banner.Build();
        return banner;
    }

    private void Build()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject barObject = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        barObject.transform.SetParent(transform, false);

        bar = barObject.GetComponent<RectTransform>();
        bar.anchorMin = new Vector2(0f, 0.5f);
        bar.anchorMax = new Vector2(1f, 0.5f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = new Vector2(0f, 200f);
        bar.anchoredPosition = new Vector2(0f, 250f);

        barImage = barObject.GetComponent<Image>();
        barImage.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(barObject.transform, false);

        textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 40f;
        text.fontSizeMax = 110f;
        text.raycastTarget = false;
    }

    private void Play(string message, Color color)
    {
        sequence?.Kill();

        text.text = message;
        text.color = color;
        barImage.color = new Color(0f, 0f, 0f, 0.65f);

        group.alpha = 1f;
        bar.localScale = new Vector3(1f, 0f, 1f);
        textRect.anchoredPosition = new Vector2(-700f, 0f);
        textRect.localScale = Vector3.one * 1.3f;

        sequence = DOTween.Sequence().SetUpdate(true);

        sequence.Append(DOTween.To(() => bar.localScale.y, y => bar.localScale = new Vector3(1f, y, 1f), 1f, 0.15f)
            .SetEase(Ease.OutQuad));
        sequence.Join(DOTween.To(() => textRect.anchoredPosition.x, x => textRect.anchoredPosition = new Vector2(x, 0f), 0f, 0.3f)
            .SetEase(Ease.OutCubic));
        sequence.Join(DOTween.To(() => textRect.localScale.x, s => textRect.localScale = Vector3.one * s, 1f, 0.3f)
            .SetEase(Ease.OutBack));

        sequence.AppendInterval(0.6f);

        sequence.Append(DOTween.To(() => textRect.anchoredPosition.x, x => textRect.anchoredPosition = new Vector2(x, 0f), 700f, 0.3f)
            .SetEase(Ease.InCubic));
        sequence.Join(DOTween.To(() => group.alpha, a => group.alpha = a, 0f, 0.3f));

        sequence.OnComplete(() => group.alpha = 0f);
    }
}
