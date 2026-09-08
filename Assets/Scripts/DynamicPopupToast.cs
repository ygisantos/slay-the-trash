using System.Collections;
using System.Collections.Generic;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;

public class DynamicPopupToast : MonoBehaviour
{
    private static DynamicPopupToast instance;

    public static DynamicPopupToast Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DynamicPopupToast>();

                if (instance == null)
                {
                    GameObject prefab =
                        Resources.Load<GameObject>("DynamicPopupToast");

                    if (prefab == null)
                    {
                        Debug.LogError(
                            "DynamicPopupToast prefab not found in Resources folder."
                        );
                        return null;
                    }

                    GameObject clone = Instantiate(prefab);
                    instance = clone.GetComponent<DynamicPopupToast>();
                }
            }

            return instance;
        }
    }

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform toastTransform;
    [SerializeField] private TMP_Text messageText;

    [Header("Animation")]
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private float slideDistance = 80f;
    [SerializeField] private AnimationCurve animationCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Inspector Debug")]
    [SerializeField] private string debugMessage = "Test popup: +1 water";

    private readonly Queue<string> messages = new Queue<string>();
    private Coroutine queueRoutine;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (toastTransform == null)
            toastTransform = transform as RectTransform;

        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>();

        if (canvasGroup == null || toastTransform == null || messageText == null)
        {
            Debug.LogError(
                "DynamicPopupToast requires a CanvasGroup, RectTransform, and TMP_Text."
            );
            return;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        visiblePosition = toastTransform.anchoredPosition;
        hiddenPosition = visiblePosition;
        canvasGroup.alpha = 0f;
        toastTransform.localScale = Vector3.one;
    }

    public void ShowToast(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        messages.Enqueue(message);
        if (queueRoutine == null)
            queueRoutine = StartCoroutine(ProcessQueue());
    }

    [Button("Debug Show Popup")]
    private void DebugShowPopup()
    {
        ShowToast(debugMessage);
    }

    [Button("Debug Queue 3 Popups")]
    private void DebugQueueThreePopups()
    {
        ShowToast(debugMessage);
        ShowToast("Second popup message");
        ShowToast("Third popup message");
    }

    private IEnumerator ProcessQueue()
    {
        while (messages.Count > 0)
        {
            string message = messages.Dequeue();
            yield return StartCoroutine(ShowMessage(message));
        }

        queueRoutine = null;
    }

    private IEnumerator ShowMessage(string message)
    {
        if (canvasGroup == null || toastTransform == null || messageText == null)
            yield break;

        messageText.text = message;

        yield return StartCoroutine(
            Animate(
                0f,
                1f,
                hiddenPosition,
                visiblePosition
            )
        );
        yield return new WaitForSecondsRealtime(visibleDuration);
        yield return StartCoroutine(
            Animate(
                1f,
                0f,
                visiblePosition,
                visiblePosition + Vector2.up * slideDistance
            )
        );
    }

    private IEnumerator Animate(
        float startAlpha,
        float endAlpha,
        Vector2 startPosition,
        Vector2 endPosition)
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / animationDuration);
            float eased = animationCurve.Evaluate(normalized);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
            toastTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                eased
            );
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        toastTransform.anchoredPosition = endPosition;
        toastTransform.localScale = Vector3.one;
    }
}
