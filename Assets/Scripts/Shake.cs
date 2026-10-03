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

        if (mainCamera != null)
            mainCamera.transform.localPosition = cameraStartPosition;

        mainCamera = Camera.main;
        if (mainCamera != null)
            cameraStartPosition = mainCamera.transform.localPosition;
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
}
