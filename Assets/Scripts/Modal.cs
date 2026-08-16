using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Modal : MonoBehaviour
{
    public enum AnimationType
    {
        None,
        Fade,
        Scale,
        FadeAndScale,
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight
    }

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform modalTransform;
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject panel;

    [Header("Open Animation")]
    [SerializeField] private AnimationType openAnimation = AnimationType.FadeAndScale;
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Close Animation")]
    [SerializeField] private AnimationType closeAnimation = AnimationType.FadeAndScale;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 500f;

    [Header("Events")]
    public UnityEvent OnOpen;
    public UnityEvent OnOpened;
    public UnityEvent OnClose;
    public UnityEvent OnClosed;

    private Coroutine animationCoroutine;
    private Vector2 originalPosition;
    private Vector3 originalScale;

    public bool IsOpen { get; private set; }
    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (modalTransform == null)
            modalTransform = transform as RectTransform;

        originalPosition = modalTransform.anchoredPosition;
        originalScale = modalTransform.localScale;

        HideImmediately();
    }

    public void Open()
    {
        if (IsOpen || IsAnimating)
            return;

        gameObject.SetActive(true);
        background.SetActive(true);
        panel.SetActive(true);

        // Reset visual state
        canvasGroup.alpha = 1f;
        modalTransform.localScale = originalScale;
        modalTransform.anchoredPosition = originalPosition;

        IsOpen = true;

        OnOpen?.Invoke();

        animationCoroutine = StartCoroutine(
            PlayAnimation(
                true,
                openAnimation,
                openDuration,
                openCurve
            )
        );
    }

    public void Close()
    {
        if (!IsOpen || IsAnimating)
            return;

        OnClose?.Invoke();

        animationCoroutine = StartCoroutine(
            PlayAnimation(
                false,
                closeAnimation,
                closeDuration,
                closeCurve
            )
        );
    }

    // =========================================================
    // TOGGLE
    // =========================================================

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private IEnumerator PlayAnimation(
        bool opening,
        AnimationType animationType,
        float duration,
        AnimationCurve curve
    )
    {
        IsAnimating = true;

        Vector2 targetPosition = originalPosition;
        Vector3 targetScale = originalScale;

        float targetAlpha = 1f;

        // Setup starting values
        if (opening)
        {
            // Reset visibility
            canvasGroup.alpha = 1f;

            switch (animationType)
            {
                case AnimationType.Fade:
                    canvasGroup.alpha = 0f;
                    break;

                case AnimationType.Scale:
                    modalTransform.localScale = Vector3.zero;
                    break;

                case AnimationType.FadeAndScale:
                    canvasGroup.alpha = 0f;
                    modalTransform.localScale = Vector3.zero;
                    break;

                case AnimationType.SlideUp:
                    modalTransform.anchoredPosition =
                        originalPosition + Vector2.down * slideDistance;
                    break;

                case AnimationType.SlideDown:
                    modalTransform.anchoredPosition =
                        originalPosition + Vector2.up * slideDistance;
                    break;

                case AnimationType.SlideLeft:
                    modalTransform.anchoredPosition =
                        originalPosition + Vector2.right * slideDistance;
                    break;

                case AnimationType.SlideRight:
                    modalTransform.anchoredPosition =
                        originalPosition + Vector2.left * slideDistance;
                    break;
            }
        }
        else
        {
            switch (animationType)
            {
                case AnimationType.Fade:
                    targetAlpha = 0f;
                    break;

                case AnimationType.Scale:
                    targetScale = Vector3.zero;
                    break;

                case AnimationType.FadeAndScale:
                    targetAlpha = 0f;
                    targetScale = Vector3.zero;
                    break;

                case AnimationType.SlideUp:
                    targetPosition =
                        originalPosition + Vector2.down * slideDistance;
                    break;

                case AnimationType.SlideDown:
                    targetPosition =
                        originalPosition + Vector2.up * slideDistance;
                    break;

                case AnimationType.SlideLeft:
                    targetPosition =
                        originalPosition + Vector2.right * slideDistance;
                    break;

                case AnimationType.SlideRight:
                    targetPosition =
                        originalPosition + Vector2.left * slideDistance;
                    break;
            }
        }

        // No animation
        if (animationType == AnimationType.None)
        {
            if (opening)
            {
                canvasGroup.alpha = 1f;
                modalTransform.localScale = originalScale;
                modalTransform.anchoredPosition = originalPosition;
            }
            else
            {
                canvasGroup.alpha = 0f;
                modalTransform.localScale = originalScale;
                modalTransform.anchoredPosition = originalPosition;
            }

            FinishAnimation(opening);
            yield break;
        }

        float elapsed = 0f;

        Vector2 startPosition = modalTransform.anchoredPosition;
        Vector3 startScale = modalTransform.localScale;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = duration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / duration);

            float eased = curve.Evaluate(progress);

            // Fade
            if (animationType == AnimationType.Fade ||
                animationType == AnimationType.FadeAndScale)
            {
                canvasGroup.alpha =
                    Mathf.Lerp(startAlpha, targetAlpha, eased);
            }

            // Scale
            if (animationType == AnimationType.Scale ||
                animationType == AnimationType.FadeAndScale)
            {
                modalTransform.localScale =
                    Vector3.Lerp(startScale, targetScale, eased);
            }

            // Slide
            if (animationType == AnimationType.SlideUp ||
                animationType == AnimationType.SlideDown ||
                animationType == AnimationType.SlideLeft ||
                animationType == AnimationType.SlideRight)
            {
                modalTransform.anchoredPosition =
                    Vector2.Lerp(startPosition, targetPosition, eased);
            }

            yield return null;
        }

        // Guarantee final values
        canvasGroup.alpha =
            (animationType == AnimationType.Fade ||
             animationType == AnimationType.FadeAndScale)
                ? targetAlpha
                : canvasGroup.alpha;

        modalTransform.localScale =
            (animationType == AnimationType.Scale ||
             animationType == AnimationType.FadeAndScale)
                ? targetScale
                : modalTransform.localScale;

        modalTransform.anchoredPosition =
            (animationType == AnimationType.SlideUp ||
             animationType == AnimationType.SlideDown ||
             animationType == AnimationType.SlideLeft ||
             animationType == AnimationType.SlideRight)
                ? targetPosition
                : modalTransform.anchoredPosition;

        FinishAnimation(opening);
    }

    private void FinishAnimation(bool opening)
    {
        IsAnimating = false;

        if (opening)
        {
            IsOpen = true;
            OnOpened?.Invoke();
        }
        else
        {
            IsOpen = false;

            OnClosed?.Invoke();

            panel.SetActive(false);
            background.SetActive(false);
            gameObject.SetActive(false);
        }

        animationCoroutine = null;
    }

    // =========================================================
    // IMMEDIATE HIDE
    // =========================================================

    public void HideImmediately()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        IsOpen = false;
        IsAnimating = false;

        canvasGroup.alpha = 0f;
        modalTransform.localScale = originalScale;
        modalTransform.anchoredPosition = originalPosition;

        panel.gameObject.SetActive(false);
        background.SetActive(false);
        gameObject.SetActive(false);
    }

    // =========================================================
    // DYNAMIC ANIMATION SETTINGS
    // =========================================================

    public void SetOpenAnimation(AnimationType animation)
    {
        openAnimation = animation;
    }

    public void SetCloseAnimation(AnimationType animation)
    {
        closeAnimation = animation;
    }

    public void SetOpenDuration(float duration)
    {
        openDuration = Mathf.Max(0f, duration);
    }

    public void SetCloseDuration(float duration)
    {
        closeDuration = Mathf.Max(0f, duration);
    }

    public void SetSlideDistance(float distance)
    {
        slideDistance = distance;
    }

    // =========================================================
    // DYNAMIC CURVES
    // =========================================================

    public void SetOpenCurve(AnimationCurve curve)
    {
        openCurve = curve;
    }

    public void SetCloseCurve(AnimationCurve curve)
    {
        closeCurve = curve;
    }
}