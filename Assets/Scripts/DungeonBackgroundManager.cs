using System;
using System.Collections;
using UnityEngine;

public class DungeonBackgroundManager : MonoBehaviour
{
    [Serializable]
    public class BackgroundLayer
    {
        public Transform layer;

        [NonSerialized] public float speed;
        [NonSerialized] public float movementRange;
        [NonSerialized] public float phaseOffset;
        [NonSerialized] public Vector3 startPosition;
    }

    [Serializable]
    public class DungeonBackground
    {
        public string dungeonName;
        public GameObject root;
        public BackgroundLayer[] layers;
    }

    private static DungeonBackgroundManager instance;

    public static DungeonBackgroundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DungeonBackgroundManager>();

                if (instance == null)
                {
                    GameObject prefab =
                        Resources.Load<GameObject>("DungeonBackgroundManager");

                    if (prefab == null)
                    {
                        Debug.LogError(
                            "DungeonBackgroundManager prefab not found in Resources folder."
                        );
                        return null;
                    }

                    GameObject clone = Instantiate(prefab);
                    instance = clone.GetComponent<DungeonBackgroundManager>();
                }
            }

            return instance;
        }
    }

    [Header("Dungeon Backgrounds")]
    [SerializeField] private DungeonBackground[] backgrounds;

    [Header("Transition")]
    [SerializeField] private CanvasGroup transitionCanvas;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Random Parallax")]
    [SerializeField] private float minimumSpeed = 0.15f;
    [SerializeField] private float maximumSpeed = 0.6f;
    [SerializeField] private float minimumMovementRange = 8f;
    [SerializeField] private float maximumMovementRange = 30f;

    private int currentIndex;
    private Coroutine transitionRoutine;

    public int CurrentIndex => currentIndex;

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

        RandomizeLayerSettings();
        CacheLayerPositions();
        HideAllBackgrounds();
    }

    private void Update()
    {
        if (backgrounds == null || currentIndex < 0 ||
            currentIndex >= backgrounds.Length)
        {
            return;
        }

        BackgroundLayer[] layers = backgrounds[currentIndex].layers;
        if (layers == null)
            return;

        for (int index = 1; index < layers.Length; index++)
        {
            BackgroundLayer backgroundLayer = layers[index];
            if (backgroundLayer == null || backgroundLayer.layer == null)
                continue;

            float offset = Mathf.Sin(
                (Time.unscaledTime + backgroundLayer.phaseOffset) *
                backgroundLayer.speed
            ) * backgroundLayer.movementRange;

            Vector3 position = backgroundLayer.startPosition;
            position.x += offset;
            backgroundLayer.layer.localPosition = position;
        }
    }

    public void ShowBackground(int index, bool animate = true)
    {
        if (!IsValidIndex(index))
            return;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        currentIndex = index;
        transitionRoutine = StartCoroutine(
            ShowBackgroundRoutine(index, animate)
        );
    }

    public void NextBackground()
    {
        if (backgrounds == null || backgrounds.Length == 0)
            return;

        int nextIndex = (currentIndex + 1) % backgrounds.Length;
        ShowBackground(nextIndex);
    }

    public void PreviousBackground()
    {
        if (backgrounds == null || backgrounds.Length == 0)
            return;

        int previousIndex = currentIndex - 1;
        if (previousIndex < 0)
            previousIndex = backgrounds.Length - 1;

        ShowBackground(previousIndex);
    }

    private IEnumerator ShowBackgroundRoutine(int index, bool animate)
    {
        if (animate && transitionCanvas != null)
            yield return Fade(0f, 1f);

        HideAllBackgrounds();
        if (backgrounds[index].root != null)
            backgrounds[index].root.SetActive(true);

        ResetLayerPositions(index);

        if (animate && transitionCanvas != null)
            yield return Fade(1f, 0f);
        else if (transitionCanvas != null)
            SetTransitionAlpha(0f);

        transitionRoutine = null;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (fadeDuration <= 0f)
        {
            SetTransitionAlpha(endAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetTransitionAlpha(
                Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration)
            );
            yield return null;
        }

        SetTransitionAlpha(endAlpha);
    }

    private void CacheLayerPositions()
    {
        if (backgrounds == null)
            return;

        foreach (DungeonBackground background in backgrounds)
        {
            if (background?.layers == null)
                continue;

            foreach (BackgroundLayer layer in background.layers)
            {
                if (layer?.layer != null)
                    layer.startPosition = layer.layer.localPosition;
            }
        }
    }

    private void RandomizeLayerSettings()
    {
        if (backgrounds == null)
            return;

        foreach (DungeonBackground background in backgrounds)
        {
            if (background?.layers == null)
                continue;

            for (int index = 0; index < background.layers.Length; index++)
            {
                BackgroundLayer layer = background.layers[index];
                if (layer == null)
                    continue;

                if (index == 0)
                {
                    layer.speed = 0f;
                    layer.movementRange = 0f;
                    layer.phaseOffset = 0f;
                    continue;
                }

                layer.speed = UnityEngine.Random.Range(
                    minimumSpeed,
                    maximumSpeed
                );
                layer.movementRange = UnityEngine.Random.Range(
                    minimumMovementRange,
                    maximumMovementRange
                );
                layer.phaseOffset = UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f
                );
            }
        }
    }

    private void ResetLayerPositions(int index)
    {
        BackgroundLayer[] layers = backgrounds[index].layers;
        if (layers == null)
            return;

        foreach (BackgroundLayer layer in layers)
        {
            if (layer?.layer != null)
                layer.layer.localPosition = layer.startPosition;
        }
    }

    private void HideAllBackgrounds()
    {
        if (backgrounds == null)
            return;

        foreach (DungeonBackground background in backgrounds)
        {
            if (background?.root != null)
                background.root.SetActive(false);
        }
    }

    private bool IsValidIndex(int index)
    {
        if (backgrounds == null || backgrounds.Length == 0)
        {
            Debug.LogWarning("No dungeon backgrounds are assigned.");
            return false;
        }

        if (index < 0 || index >= backgrounds.Length)
        {
            Debug.LogWarning("Dungeon background index is out of range.");
            return false;
        }

        return backgrounds[index] != null && backgrounds[index].root != null;
    }

    private void SetTransitionAlpha(float alpha)
    {
        if (transitionCanvas != null)
            transitionCanvas.alpha = Mathf.Clamp01(alpha);
    }
}
