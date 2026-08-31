using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    private static LoadingScreenManager instance;

    public static LoadingScreenManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LoadingScreenManager>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>("LoadingScreenManager");

                    if (singletonPrefab == null)
                    {
                        GameObject go =
                            new GameObject("LoadingScreenManager");

                        instance = go.AddComponent<LoadingScreenManager>();
                        DontDestroyOnLoad(go);
                        return instance;
                    }

                    GameObject clone =
                        Instantiate(singletonPrefab);

                    instance = clone.GetComponent<LoadingScreenManager>();
                }
            }

            return instance;
        }
    }

    private Canvas canvas;
    private GameObject root;
    private Image background;
    private Image spinner;
    private TextMeshProUGUI messageText;
    private Coroutine spinRoutine;
    private Coroutine pulseRoutine;
    private Coroutine hideRoutine;
    private System.Action onHidden;

    private const float SpinnerSpeed = 180f;
    private const float MinVisibleTime = 1f;
    private float showStartedAt;
    private bool isLoading;

    public bool IsLoadingActive => isLoading;

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

        BuildUI();
        Hide();
    }

    private void BuildUI()
    {
        if (root == null)
        {
            Transform existingRoot = transform.Find("LoadingRoot");
            if (existingRoot != null)
            {
                root = existingRoot.gameObject;
            }
            else
            {
                root = new GameObject("LoadingRoot");
                root.transform.SetParent(transform, false);
            }
        }

        canvas = root.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
        }

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (root.GetComponent<GraphicRaycaster>() == null)
            root.AddComponent<GraphicRaycaster>();

        if (background == null)
        {
            Transform existingBackground = root.transform.Find("Background");
            if (existingBackground != null)
            {
                background = existingBackground.GetComponent<Image>();
            }
            else
            {
                GameObject backgroundObj =
                    new GameObject("Background");

                backgroundObj.transform.SetParent(root.transform, false);
                background = backgroundObj.AddComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.62f);

                RectTransform bgRect = background.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
            }
        }

        if (spinner == null)
        {
            Transform existingSpinner = root.transform.Find("Spinner");
            if (existingSpinner != null)
            {
                spinner = existingSpinner.GetComponent<Image>();
            }
            else
            {
                GameObject spinnerHolder = new GameObject("Spinner");
                spinnerHolder.transform.SetParent(root.transform, false);

                spinner = spinnerHolder.AddComponent<Image>();
                spinner.color = new Color(1f, 1f, 1f, 1f);

                RectTransform spinnerRect = spinner.GetComponent<RectTransform>();
                spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
                spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
                spinnerRect.sizeDelta = new Vector2(90f, 90f);
                spinnerRect.anchoredPosition = new Vector2(0f, 60f);

                spinner.type = Image.Type.Filled;
                spinner.fillMethod = Image.FillMethod.Radial360;
                spinner.fillOrigin = (int)Image.Origin360.Top;
                spinner.fillAmount = 0.75f;
            }
        }

        if (messageText == null)
        {
            Transform existingMessage = root.transform.Find("Message");
            if (existingMessage != null)
            {
                messageText = existingMessage.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject textHolder = new GameObject("Message");
                textHolder.transform.SetParent(root.transform, false);

                messageText = textHolder.AddComponent<TextMeshProUGUI>();
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.fontSize = 28;
                messageText.color = Color.white;
                messageText.text = "Loading...";

                RectTransform textRect = messageText.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.sizeDelta = new Vector2(700f, 80f);
                textRect.anchoredPosition = new Vector2(0f, -40f);
            }
        }

        root.SetActive(false);
    }

    public void Show(string message = "Loading...")
    {
        if (isLoading)
            return;

        if (root == null)
            BuildUI();

        if (messageText != null)
            messageText.text = message;

        if (root != null)
            root.SetActive(true);

        isLoading = true;
        showStartedAt = Time.unscaledTime;

        if (spinRoutine != null)
            StopCoroutine(spinRoutine);

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        spinRoutine = StartCoroutine(SpinRoutine());
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    public void Hide()
    {
        Hide(null);
    }

    public void Hide(System.Action afterHidden)
    {
        if (!isLoading)
        {
            afterHidden?.Invoke();
            return;
        }

        onHidden = afterHidden;

        float elapsed = Time.unscaledTime - showStartedAt;

        if (elapsed < MinVisibleTime)
        {
            float remaining = MinVisibleTime - elapsed;

            if (hideRoutine != null)
                StopCoroutine(hideRoutine);

            hideRoutine = StartCoroutine(HideAfterDelay(remaining));
            return;
        }

        FinishHide();
    }

    public void RunWithLoading(
        string message,
        System.Action action,
        System.Action onComplete = null)
    {
        if (action == null)
        {
            onComplete?.Invoke();
            return;
        }

        Show(message);

        StartCoroutine(ExecuteWithLoading(action, onComplete));
    }

    private IEnumerator ExecuteWithLoading(
        System.Action action,
        System.Action onComplete)
    {
        action.Invoke();

        while (isLoading)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        FinishHide();
    }

    private void FinishHide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (spinner != null)
            spinner.rectTransform.localRotation = Quaternion.identity;

        if (spinner != null)
            spinner.rectTransform.localScale = Vector3.one;

        isLoading = false;

        if (root != null)
            root.SetActive(false);

        System.Action completed = onHidden;
        onHidden = null;
        completed?.Invoke();
    }

    private IEnumerator SpinRoutine()
    {
        while (root != null && root.activeSelf)
        {
            if (spinner != null)
            {
                spinner.rectTransform.Rotate(
                    Vector3.forward,
                    SpinnerSpeed * Time.unscaledDeltaTime
                );
            }

            yield return null;
        }
    }

    private IEnumerator PulseRoutine()
    {
        while (root != null && root.activeSelf)
        {
            if (spinner != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.12f;
                spinner.rectTransform.localScale =
                    new Vector3(pulse, pulse, 1f);
            }

            yield return null;
        }
    }
}
