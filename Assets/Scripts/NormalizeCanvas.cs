using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public class NormalizeCanvas : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool refreshOnSceneLoaded = true;
    [SerializeField] private float planeDistance = 100f;

    private Canvas canvas;
    private Coroutine cameraRoutine;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        Normalize();
        StartCameraRefresh();
    }

    private void OnEnable()
    {
        if (refreshOnSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;

        Normalize();
        StartCameraRefresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (cameraRoutine != null)
        {
            StopCoroutine(cameraRoutine);
            cameraRoutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Normalize();
        StartCameraRefresh();
    }

    public void Normalize()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            targetCamera = Camera.main;

        if (targetCamera == null)
            targetCamera = FindFirstObjectByType<Camera>();

        canvas.worldCamera = targetCamera;

        if (targetCamera != null)
        {
            float maximumDistance =
                Mathf.Max(
                    targetCamera.nearClipPlane + 0.01f,
                    targetCamera.farClipPlane - 0.01f
                );

            canvas.planeDistance = Mathf.Clamp(
                planeDistance,
                targetCamera.nearClipPlane + 0.01f,
                maximumDistance
            );
        }

        if (targetCamera == null)
        {
            Debug.LogWarning(
                $"NormalizeCanvas could not find a camera for {name}."
            );
        }
    }

    private void StartCameraRefresh()
    {
        if (cameraRoutine != null)
            StopCoroutine(cameraRoutine);

        cameraRoutine = StartCoroutine(RefreshCameraRoutine());
    }

    private System.Collections.IEnumerator RefreshCameraRoutine()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Normalize();

            if (canvas != null && canvas.worldCamera != null)
            {
                cameraRoutine = null;
                yield break;
            }

            yield return null;
        }

        cameraRoutine = null;
    }
}
