using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class PostProcessingManager : MonoBehaviour
{
    private static PostProcessingManager instance;

    public static PostProcessingManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance =
                    FindFirstObjectByType<PostProcessingManager>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>(
                            "PostProcessingManager"
                        );

                    if (singletonPrefab == null)
                    {
                        Debug.LogError(
                            "PostProcessingManager prefab not found in Resources folder."
                        );

                        return null;
                    }

                    GameObject clone =
                        Instantiate(singletonPrefab);

                    instance =
                        clone.GetComponent<PostProcessingManager>();
                }
            }

            return instance;
        }
    }


    [Header("Volume")]
    [SerializeField] private Volume volume;


    [Header("Camera Settings")]
    [SerializeField] private bool enablePostProcessing = true;
    [SerializeField] private CameraRenderType renderType =
        CameraRenderType.Base;
    [SerializeField] private bool orthographic = true;
    [SerializeField] private float orthographicSize = 5f;
    [SerializeField] private AntialiasingMode antiAliasing =
        AntialiasingMode.FastApproximateAntialiasing;


    private DepthOfField depthOfField;


    // =========================================================
    // SINGLETON
    // =========================================================

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);

            Initialize();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        DisableDepthOfField();

        SetupMainCamera();
    }


    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }


    // =========================================================
    // SCENE LOADED
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        SetupMainCamera();
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Initialize()
    {
        if (volume == null)
        {
            Debug.LogError(
                "PostProcessingManager: Volume is not assigned."
            );

            return;
        }

        if (!volume.profile.TryGet(
                out depthOfField))
        {
            Debug.LogError(
                "PostProcessingManager: " +
                "Depth of Field was not found in the Volume Profile."
            );
        }
    }


    // =========================================================
    // MAIN CAMERA
    // =========================================================

    private void SetupMainCamera()
    {
        // Always find the Main Camera of the current scene
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning(
                "PostProcessingManager: Main Camera was not found."
            );

            return;
        }


        // -----------------------------------------------------
        // Standard Camera Settings
        // -----------------------------------------------------

        mainCamera.orthographic = orthographic;
        mainCamera.orthographicSize = orthographicSize;


        // -----------------------------------------------------
        // URP Camera Settings
        // -----------------------------------------------------

        UniversalAdditionalCameraData cameraData =
            mainCamera.GetUniversalAdditionalCameraData();

        if (cameraData == null)
        {
            Debug.LogError(
                "PostProcessingManager: " +
                "UniversalAdditionalCameraData was not found."
            );

            return;
        }


        cameraData.renderType = renderType;

        cameraData.renderPostProcessing =
            enablePostProcessing;

        cameraData.antialiasing =
            antiAliasing;
    }


    // =========================================================
    // DEPTH OF FIELD
    // =========================================================

    public void EnableDepthOfField()
    {
        if (depthOfField == null)
            return;

        depthOfField.active = true;
    }


    public void DisableDepthOfField()
    {
        if (depthOfField == null)
            return;

        depthOfField.active = false;
    }


    public void SetDepthOfField(bool enabled)
    {
        if (depthOfField == null)
            return;

        depthOfField.active = enabled;
    }
}
