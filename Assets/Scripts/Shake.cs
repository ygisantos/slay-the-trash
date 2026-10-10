using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shake : MonoBehaviour
{
    private static Shake instance;

    public static Shake Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<Shake>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>("Shake");

                    if (singletonPrefab == null)
                    {
                        Debug.LogError(
                            "Shake prefab not found in Resources folder."
                        );
                        return null;
                    }

                    GameObject clone = Instantiate(singletonPrefab);
                    instance = clone.GetComponent<Shake>();
                }
            }

            return instance;
        }
    }

    [SerializeField, Min(0f)] private float defaultDuration = 0.18f;
    [SerializeField, Min(0f)] private float defaultMagnitude = 0.06f;

    private Camera mainCamera;
    private Vector3 cameraStartPosition;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshCamera();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshCamera();
    }

    private void RefreshCamera()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
            zoomRoutine = null;
        }

        if (mainCamera != null)
            mainCamera.transform.localPosition = cameraStartPosition;

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraStartPosition = mainCamera.transform.localPosition;
            baseOrthoSize = mainCamera.orthographic ? mainCamera.orthographicSize : -1f;
        }
    }

    public void ShakeCamera(float duration = -1f, float magnitude = -1f)
    {
        if (mainCamera == null)
            return;

        duration = duration < 0f ? defaultDuration : duration;
        magnitude = magnitude < 0f ? defaultMagnitude : magnitude;

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            mainCamera.transform.localPosition = cameraStartPosition;
        }

        shakeRoutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float strength = 1f - elapsed / duration;
            Vector2 offset = Random.insideUnitCircle * magnitude * strength;
            mainCamera.transform.localPosition = cameraStartPosition +
                new Vector3(offset.x, offset.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = cameraStartPosition;
        shakeRoutine = null;
    }

    // ─── Camera Zoom ──────────────────────────────────────────────────────────────
    // Punches the orthographic size in toward a world-space target, then eases back.

    [SerializeField, Min(0f)] private float defaultZoomAmount    = 1.2f;  // how many units to shrink ortho size
    [SerializeField, Min(0f)] private float defaultZoomInTime    = 0.12f; // seconds to zoom in
    [SerializeField, Min(0f)] private float defaultZoomHoldTime  = 0.08f; // seconds held at peak zoom
    [SerializeField, Min(0f)] private float defaultZoomOutTime   = 0.22f; // seconds to ease back

    private float baseOrthoSize = -1f;
    private Coroutine zoomRoutine;

    // Zoom toward a world-space position (pass the target's transform.position).
    public void ZoomTo(
        Vector3 worldTarget,
        float zoomAmount   = -1f,
        float zoomInTime   = -1f,
        float holdTime     = -1f,
        float zoomOutTime  = -1f)
    {
        if (mainCamera == null || !mainCamera.orthographic)
            return;

        zoomAmount  = zoomAmount  < 0f ? defaultZoomAmount   : zoomAmount;
        zoomInTime  = zoomInTime  < 0f ? defaultZoomInTime   : zoomInTime;
        holdTime    = holdTime    < 0f ? defaultZoomHoldTime  : holdTime;
        zoomOutTime = zoomOutTime < 0f ? defaultZoomOutTime  : zoomOutTime;

        if (zoomRoutine != null)
            StopCoroutine(zoomRoutine);

        zoomRoutine = StartCoroutine(ZoomCoroutine(worldTarget, zoomAmount, zoomInTime, holdTime, zoomOutTime));
    }

    private IEnumerator ZoomCoroutine(
        Vector3 worldTarget,
        float zoomAmount,
        float zoomInTime,
        float holdTime,
        float zoomOutTime)
    {
        if (baseOrthoSize < 0f)
            baseOrthoSize = mainCamera.orthographicSize;

        float startSize    = mainCamera.orthographicSize;
        float targetSize   = Mathf.Max(0.5f, baseOrthoSize - zoomAmount);
        Vector3 startPos   = mainCamera.transform.position;

        // Clamp pan so the camera doesn't leave a comfortable range.
        Vector3 panTarget  = Vector3.Lerp(startPos, new Vector3(worldTarget.x, worldTarget.y, startPos.z), 0.3f);

        // ── Zoom in ──
        float elapsed = 0f;
        while (elapsed < zoomInTime)
        {
            float t = elapsed / zoomInTime;
            float ease = t * t;
            mainCamera.orthographicSize      = Mathf.Lerp(startSize, targetSize, ease);
            mainCamera.transform.position    = Vector3.Lerp(startPos, panTarget, ease);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.orthographicSize   = targetSize;
        mainCamera.transform.position = panTarget;

        // ── Hold ──
        yield return new WaitForSeconds(holdTime);

        // ── Zoom out ──
        elapsed = 0f;
        while (elapsed < zoomOutTime)
        {
            float t = elapsed / zoomOutTime;
            float ease = 1f - (1f - t) * (1f - t);
            mainCamera.orthographicSize      = Mathf.Lerp(targetSize, baseOrthoSize, ease);
            mainCamera.transform.position    = Vector3.Lerp(panTarget, startPos, ease);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.orthographicSize   = baseOrthoSize;
        mainCamera.transform.position = startPos;
        zoomRoutine = null;
    }
}
