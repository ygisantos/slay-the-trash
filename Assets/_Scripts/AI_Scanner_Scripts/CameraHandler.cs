using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TMPro;
using Unity.Barracuda;
using Sirenix.OdinInspector;

public class CameraHandler : MonoBehaviour
{
    // How the scanner should react once a prediction is made.
    // Collection: legacy flow, transitions to ThrowingInstructionsScene (add new collection).
    // DailyQuest: stays in this modal and reports the result directly instead of changing scenes.
    public enum ScanMode
    {
        Collection,
        DailyQuest
    }
    [Header("Scan Debug")]
    [SerializeField] private RectTransform scanSquareGuide;
    [SerializeField] private bool saveFullFrameForDebug = true;

    [Tooltip("Draw the scan square guide over the camera feed. Turn on for aiming/debugging.")]
    [SerializeField] private bool showSquare = false;

    // Runtime-generated fallback frame sprite for the scan square.
    private Sprite scanSquareSprite;
    private Texture2D scanSquareTexture;

    [Header("UI")]
    public RawImage cameraFeedDisplay;
    public Text predictionText;
    public Button captureButton;

    [Tooltip("Optional TMP dropdown used to switch Barracuda worker backends at runtime.")]
    public TMP_Dropdown workerTypeDropdown;

    [Header("AI")]
    public NNModel modelAsset;

    [Header("Scene")]
    public AIScannerSceneHandlerScript sceneHandler;

    [Header("Scan Mode")]
    public ScanMode scanMode = ScanMode.Collection;

    private WebCamTexture webCamTexture;
    private bool isCameraReady = false;

    // Barracuda
    private Model runtimeModel;
    private IWorker worker;
    private WorkerFactory.Type selectedWorkerType = WorkerFactory.Type.CSharpBurst;
    private readonly List<WorkerFactory.Type> workerTypeOptions = new List<WorkerFactory.Type>();
    private bool updatingWorkerDropdown = false;

    // Matches camera_test (1).py preprocessing.
    private const float CENTER_CROP_FRACTION = 0.65f;
    private const float CONFIDENCE_THRESHOLD = 0.70f;
    private const float MARGIN_THRESHOLD = 0.20f;
    private static readonly float[] INPUT_MEAN = { 0.485f, 0.456f, 0.406f };
    private static readonly float[] INPUT_STD = { 0.229f, 0.224f, 0.225f };

    // Model output order (updated model)
    private readonly string[] classLabels =
    {
        // == IANY ==
        // "can",
        // "glass",
        // "organic",
        // "other",
        // "paper",
        // "plastic_bottle",
        // "plastic_other"

        // == ALJ ==
        "e-waste",
        "food-waste",
        "metal",
        "paper",
        "plastic"
    };

    // Maps each raw model class (by index) to one of the 3 bin categories
    [Header("Label Mapping (edit when swapping models)")]
    private string[] categoryLabels =
    {
        // == IANY ==
        // "recyclable",  // can
        // "recyclable",  // glass
        // "biological",  // organic
        // "non-bio",     // other
        // "recyclable",  // paper
        // "recyclable",  // plastic_bottle
        // "recyclable"   // plastic_other

        // == ALJ ==
        "non-bio",      // e_waste
        "biological",  // food_waste
        "recyclable",   // metal
        "recyclable",  // paper
        "recyclable",  // plastic
    };

    // Maps each raw model class (by index) to one of the 5 display categories
    private string[] displayLabels =
    {
        //  == IANY ==
        // "metal",       // can
        // "glass",       // glass
        // "food waste",  // organic
        // "trash",       // other
        // "paper",       // paper
        // "plastic",     // plastic_bottle
        // "plastic"      // plastic_other

        // == ALJ ==
        "e-waste",     // e_waste
        "food waste",  // food_waste
        "metal",        // metal
        "paper",       // paper
        "plastic",     // plastic
    };

    private Texture2D reusableTexture;
    private bool isProcessing = false;

    private const int MODEL_INPUT_RESOLUTION = 224;

    void Start()
    {
        StartCoroutine(InitializeAI());
        StartCamera();
    }

    // Restarts the camera whenever this prefab is re-enabled (e.g. modal reopened).
    void OnEnable()
    {
        if (isAiInitialized)
        {
            if (workerTypeDropdown != null)
            {
                workerTypeDropdown.onValueChanged.RemoveListener(OnWorkerDropdownChanged);
                workerTypeDropdown.onValueChanged.AddListener(OnWorkerDropdownChanged);
            }
            StartCamera();
        }
    }

    private float[] Softmax(float[] logits)
    {
        float maxLogit = float.MinValue;

        for (int i = 0; i < logits.Length; i++)
        {
            if (logits[i] > maxLogit)
                maxLogit = logits[i];
        }

        float sum = 0f;
        float[] probabilities = new float[logits.Length];

        for (int i = 0; i < logits.Length; i++)
        {
            probabilities[i] =
                Mathf.Exp(logits[i] - maxLogit);

            sum += probabilities[i];
        }

        for (int i = 0; i < probabilities.Length; i++)
        {
            probabilities[i] /= sum;
        }

        return probabilities;
    }

    private bool isAiInitialized = false;

    // ================================================================
    // START/RESTART CAMERA (safe to call again, e.g. when a modal reopens)
    // ================================================================

    private Coroutine cameraRoutine;

    public void StartCamera()
    {
        if (cameraRoutine != null)
        {
            StopCoroutine(cameraRoutine);
        }

        cameraRoutine = StartCoroutine(InitializeCamera());
    }

    IEnumerator InitializeCamera()
    {
        // ============================================================
        // CAMERA PERMISSION
        // ============================================================

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(
                UserAuthorization.WebCam
            );
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam) ||
            WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("No camera devices found or camera permission denied.");

            DisplayPrediction(
                "Camera Error: No device or permission.",
                Color.red
            );

            yield break;
        }

        // ============================================================
        // START CAMERA (reuse the existing device if already created)
        // ============================================================

        if (webCamTexture == null)
        {
            WebCamDevice device = WebCamTexture.devices[0];

            foreach (var camDevice in WebCamTexture.devices)
            {
                if (!camDevice.isFrontFacing)
                {
                    device = camDevice;
                    break;
                }
            }

            webCamTexture = new WebCamTexture(device.name);

            cameraFeedDisplay.texture = webCamTexture;
        }

        if (!webCamTexture.isPlaying)
        {
            webCamTexture.Play();
        }

        // Wait for camera initialization
        while (webCamTexture.width <= 16 ||
               webCamTexture.height <= 16)
        {
            DisplayPrediction(
                "Initializing Camera...",
                Color.yellow
            );

            yield return null;
        }

        isCameraReady = true;

        ApplyCameraFeedDisplaySettings();
    }

    // ================================================================
    // LOAD AI MODEL (runs once)
    // ================================================================

    IEnumerator InitializeAI()
    {
        // ============================================================
        // CHECK BARRACUDA MODEL
        // ============================================================

        if (modelAsset == null)
        {
            Debug.LogError(
                "NNModel is not assigned in the Inspector!"
            );

            DisplayPrediction(
                "AI Error: Model not assigned.",
                Color.red
            );

            yield break;
        }

        // ============================================================
        // LOAD BARRACUDA MODEL
        // ============================================================

        try
        {
            Debug.Log("Loading Barracuda model...");

            runtimeModel = ModelLoader.Load(modelAsset);

            // GPU backend
            ConfigureWorkerDropdown();
            CreateWorker(selectedWorkerType);

            Debug.Log($"Barracuda model loaded. Worker: {selectedWorkerType}");

            DisplayPrediction(
                "Camera & AI Ready!",
                Color.green
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Failed to load Barracuda model: " + e
            );

            DisplayPrediction(
                "AI Error: Failed to load model.",
                Color.red
            );

            yield break;
        }

        // ============================================================
        // CAPTURE BUTTON
        // ============================================================

        if (captureButton != null)
        {
            captureButton.onClick.RemoveListener(ProcessFrameForAI);
            captureButton.onClick.AddListener(ProcessFrameForAI);
        }

        // ============================================================
        // REUSABLE 224x224 TEXTURE
        // ============================================================

        reusableTexture = new Texture2D(
            MODEL_INPUT_RESOLUTION,
            MODEL_INPUT_RESOLUTION,
            TextureFormat.RGB24,
            false
        );

        isAiInitialized = true;
    }

    // ================================================================
    // RUNTIME WORKER BACKEND DROPDOWN
    // ================================================================

    private void ConfigureWorkerDropdown()
    {
        if (workerTypeDropdown == null)
        {
            Debug.Log("Worker dropdown is not assigned; using CSharpBurst by default.");
            selectedWorkerType = WorkerFactory.Type.CSharpBurst;
            return;
        }

        workerTypeDropdown.onValueChanged.RemoveListener(OnWorkerDropdownChanged);
        workerTypeDropdown.ClearOptions();
        workerTypeOptions.Clear();

        // Build choices from this installed Barracuda version, so unavailable enum
        // values are never referenced by name in code.
        Array enumValues = Enum.GetValues(typeof(WorkerFactory.Type));
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (object enumValue in enumValues)
        {
            WorkerFactory.Type workerType = (WorkerFactory.Type)enumValue;
            workerTypeOptions.Add(workerType);
            options.Add(new TMP_Dropdown.OptionData(GetWorkerDisplayName(workerType)));
        }

        workerTypeDropdown.AddOptions(options);

        int defaultIndex = workerTypeOptions.IndexOf(WorkerFactory.Type.CSharpBurst);
        if (defaultIndex < 0) defaultIndex = 0;
        selectedWorkerType = workerTypeOptions[defaultIndex];

        updatingWorkerDropdown = true;
        workerTypeDropdown.SetValueWithoutNotify(defaultIndex);
        workerTypeDropdown.RefreshShownValue();
        updatingWorkerDropdown = false;
        workerTypeDropdown.onValueChanged.AddListener(OnWorkerDropdownChanged);

        Debug.Log("Available Barracuda worker types: " + string.Join(", ", workerTypeOptions));
    }

    private string GetWorkerDisplayName(WorkerFactory.Type workerType)
    {
        string name = workerType.ToString();
        switch (name)
        {
            case "CSharpBurst": return "CSharpBurst — CPU Burst";
            case "CSharp": return "CSharp — CPU";
            case "ComputePrecompiled": return "ComputePrecompiled — GPU Compute";
            case "Compute": return "Compute — GPU Compute";
            case "Auto": return "Auto — automatic selection";
            case "Reference": return "Reference — reference/debug";
            default: return name;
        }
    }

    public void OnWorkerDropdownChanged(int dropdownIndex)
    {
        if (updatingWorkerDropdown || dropdownIndex < 0 || dropdownIndex >= workerTypeOptions.Count)
            return;

        if (isProcessing)
        {
            Debug.LogWarning("Cannot change the inference worker while a scan is running.");
            RestoreWorkerDropdownSelection();
            return;
        }

        WorkerFactory.Type requestedType = workerTypeOptions[dropdownIndex];
        if (requestedType == selectedWorkerType && worker != null)
            return;

        try
        {
            CreateWorker(requestedType);
            selectedWorkerType = requestedType;
            DisplayPrediction($"Worker changed to: {GetWorkerDisplayName(selectedWorkerType)}", Color.cyan);
            Debug.Log($"Inference worker changed to {selectedWorkerType}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not create worker {requestedType}: {e}");
            DisplayPrediction($"Worker unavailable: {requestedType}", Color.red);
            RestoreWorkerDropdownSelection();
        }
    }

    private void CreateWorker(WorkerFactory.Type workerType)
    {
        if (runtimeModel == null)
            throw new InvalidOperationException("Cannot create an inference worker before the model is loaded.");

        // Create first so a failed backend selection doesn't destroy the current working worker.
        IWorker newWorker = WorkerFactory.CreateWorker(workerType, runtimeModel);
        IWorker oldWorker = worker;
        worker = newWorker;
        if (oldWorker != null)
            oldWorker.Dispose();
        selectedWorkerType = workerType;
    }

    private void RestoreWorkerDropdownSelection()
    {
        if (workerTypeDropdown == null) return;
        int currentIndex = workerTypeOptions.IndexOf(selectedWorkerType);
        if (currentIndex < 0) return;
        updatingWorkerDropdown = true;
        workerTypeDropdown.SetValueWithoutNotify(currentIndex);
        workerTypeDropdown.RefreshShownValue();
        updatingWorkerDropdown = false;
    }

    // ================================================================
    // CAMERA DISPLAY
    // ================================================================

    void ApplyCameraFeedDisplaySettings()
    {
        if (webCamTexture == null ||
            cameraFeedDisplay == null)
        {
            return;
        }

        Debug.Log(
            $"{webCamTexture.width} {webCamTexture.height}"
        );

        int rotation = webCamTexture.videoRotationAngle;
        bool isQuarterTurn = rotation == 90 || rotation == 270;
        float displayWidth = isQuarterTurn
            ? webCamTexture.height
            : webCamTexture.width;
        float displayHeight = isQuarterTurn
            ? webCamTexture.width
            : webCamTexture.height;

        float cameraSizeMult =
            1600f / displayWidth;

        Debug.Log(cameraSizeMult);

        cameraFeedDisplay.rectTransform.sizeDelta =
            new Vector2(
                displayWidth,
                displayHeight
            ) * cameraSizeMult;

        cameraFeedDisplay.rectTransform.localEulerAngles =
            new Vector3(0f, 0f, -rotation);
        cameraFeedDisplay.uvRect = webCamTexture.videoVerticallyMirrored
            ? new Rect(0f, 1f, 1f, -1f)
            : new Rect(0f, 0f, 1f, 1f);

        ApplyScanSquareGuideSize(displayWidth, displayHeight, cameraSizeMult);
    }

    // ================================================================
    // SCAN SQUARE GUIDE
    //
    // The guide is parented to the camera feed so it inherits the same
    // rotation, and sized to CENTER_CROP_FRACTION of the frame's shorter
    // side — the exact region RunAIPrediction crops and sends to the model.
    // ================================================================

    private void ApplyScanSquareGuideSize(
        float displayWidth,
        float displayHeight,
        float cameraSizeMult)
    {
        if (scanSquareGuide == null)
        {
            return;
        }

        if (scanSquareGuide.parent != cameraFeedDisplay.rectTransform)
        {
            scanSquareGuide.SetParent(cameraFeedDisplay.rectTransform, false);
        }

        // The crop is the centre square of the shorter frame dimension, so the
        // guide is the same fraction of the preview's shorter displayed side.
        float shorterDisplayedSide =
            Mathf.Min(displayWidth, displayHeight) * cameraSizeMult;
        float guideSize = shorterDisplayedSide * CENTER_CROP_FRACTION;

        scanSquareGuide.anchorMin = new Vector2(0.5f, 0.5f);
        scanSquareGuide.anchorMax = new Vector2(0.5f, 0.5f);
        scanSquareGuide.pivot = new Vector2(0.5f, 0.5f);
        scanSquareGuide.anchoredPosition = Vector2.zero;
        scanSquareGuide.localRotation = Quaternion.identity;
        scanSquareGuide.sizeDelta = new Vector2(guideSize, guideSize);

        EnsureScanSquareIsVisible();

        // Kept sized and correct even while hidden, so flipping showSquare in the
        // Inspector is all that is needed to bring it back.
        scanSquareGuide.gameObject.SetActive(showSquare);

        Debug.Log(
            $"[SCAN DEBUG] Scan square guide sized to {guideSize:F1} " +
            $"(crop fraction {CENTER_CROP_FRACTION} of {shorterDisplayedSide:F1})"
        );
    }

    // The packaged ScanSquareGuide has an Image with no sprite assigned, which
    // Unity does not render, so build a hollow frame sprite when one is missing.
    // A sprite already assigned in the Inspector is left untouched.
    private void EnsureScanSquareIsVisible()
    {
        Image guideImage = scanSquareGuide.GetComponent<Image>();

        if (guideImage == null)
        {
            return;
        }

        if (guideImage.sprite == null)
        {
            guideImage.sprite = GetOrCreateScanSquareSprite();
            guideImage.type = Image.Type.Sliced;
        }

        if (guideImage.color.a < 0.6f)
        {
            guideImage.color = new Color(
                guideImage.color.r,
                guideImage.color.g,
                guideImage.color.b,
                0.9f
            );
        }
    }

    private Sprite GetOrCreateScanSquareSprite()
    {
        if (scanSquareSprite != null)
        {
            return scanSquareSprite;
        }

        const int textureSize = 64;
        const int sliceBorder = 2;

        scanSquareTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false
        );
        scanSquareTexture.wrapMode = TextureWrapMode.Clamp;
        scanSquareTexture.filterMode = FilterMode.Point;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color edge = Color.white;
        Color[] pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool isEdge =
                    x < sliceBorder ||
                    y < sliceBorder ||
                    x >= textureSize - sliceBorder ||
                    y >= textureSize - sliceBorder;

                pixels[y * textureSize + x] = isEdge ? edge : clear;
            }
        }

        scanSquareTexture.SetPixels(pixels);
        scanSquareTexture.Apply();

        scanSquareSprite = Sprite.Create(
            scanSquareTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder)
        );
        scanSquareSprite.name = "ScanSquareGuideFrame";

        return scanSquareSprite;
    }

    // ================================================================
    // CAPTURE BUTTON
    // ================================================================

    public void ProcessFrameForAI()
    {
        if (!isCameraReady ||
            webCamTexture == null ||
            !webCamTexture.isPlaying ||
            isProcessing)
        {
            return;
        }

        // Set immediately (not inside the coroutine) so a second tap during the
        // 0.5s delay below can't start a second overlapping prediction.
        isProcessing = true;

        StartCoroutine(RunAIPrediction());

        SoundManager.Instance.PlaySound(SoundType.CAMERA);
    }

    // ================================================================
    // CAPTURE + PREPROCESS IMAGE
    // ================================================================

    IEnumerator RunAIPrediction()
    {
        Texture2D fullFrameCaptured = null;
        Texture2D rotatedFullFrame = null;
        RenderTexture tempCamRT = null;
        RenderTexture resizeRT = null;
        RenderTexture previousActive = RenderTexture.active;

        // Keep the short capture delay outside the try/catch block (C# iterator rule).
        yield return new WaitForSeconds(.5f);

        try
        {
            if (webCamTexture == null || !webCamTexture.isPlaying || reusableTexture == null)
            {
                DisplayPrediction("Camera is not ready. Please try again.", Color.red);
                yield break;
            }

            DisplayPrediction("Processing...", Color.yellow);

            // Capture the latest camera frame.
            tempCamRT = RenderTexture.GetTemporary(webCamTexture.width, webCamTexture.height, 24);
            Graphics.Blit(webCamTexture, tempCamRT);
            RenderTexture.active = tempCamRT;

            fullFrameCaptured = new Texture2D(tempCamRT.width, tempCamRT.height, TextureFormat.RGB24, false);
            fullFrameCaptured.ReadPixels(new Rect(0, 0, tempCamRT.width, tempCamRT.height), 0, 0);
            fullFrameCaptured.Apply();

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(tempCamRT);
            tempCamRT = null;

            // Orient the captured frame exactly like the on-screen viewfinder.
            // Previously this hard-coded a 90 deg clockwise turn, which does not
            // match RawImage.localEulerAngles = -videoRotationAngle and left the
            // model looking at a rotated/mirrored copy of what the user framed.
            rotatedFullFrame = BuildFrameMatchingPreview(fullFrameCaptured);
            Destroy(fullFrameCaptured);
            fullFrameCaptured = null;

            Debug.Log(
                $"[SCAN DEBUG] Camera orientation: rotationAngle={webCamTexture.videoRotationAngle}, " +
                $"verticallyMirrored={webCamTexture.videoVerticallyMirrored}, " +
                $"frame={rotatedFullFrame.width}x{rotatedFullFrame.height}"
            );

            string debugFolder;

            // These PNG dumps are ~780 KB and several file writes per scan, so
            // they are only produced in the collection flow (the scene where the
            // viewfinder can be compared against the frame sent to the model).
            bool saveDebugImages =
                saveFullFrameForDebug && scanMode == ScanMode.Collection;

            // Save the full rotated camera frame for comparison.
            if (saveDebugImages)
            {
                debugFolder = Path.Combine(
                    Application.persistentDataPath,
                    "Compared"
                );

                Directory.CreateDirectory(debugFolder);

                string fullFramePath = Path.Combine(
                    debugFolder,
                    "full_camera_frame.png"
                );

                File.WriteAllBytes(
                    fullFramePath,
                    rotatedFullFrame.EncodeToPNG()
                );

                Debug.Log($"[SCAN DEBUG] Full frame saved: {fullFramePath}");
            }

            // Python reference: centered square crop covering 65% of the shorter dimension.
            int cropSize = Mathf.Max(1, Mathf.RoundToInt(
                Mathf.Min(rotatedFullFrame.width, rotatedFullFrame.height) * CENTER_CROP_FRACTION));
            int cropX = (rotatedFullFrame.width - cropSize) / 2;
            int cropY = (rotatedFullFrame.height - cropSize) / 2;

            if (saveDebugImages)
            {
                Texture2D cropDebug = CreateCropDebugImage(
                    rotatedFullFrame,
                    cropX,
                    cropY,
                    cropSize
                );

                debugFolder = Path.Combine(
                    Application.persistentDataPath,
                    "Compared"
                );

                Directory.CreateDirectory(debugFolder);

                string cropDebugPath = Path.Combine(
                    debugFolder,
                    "crop_boundary_debug.png"
                );

                File.WriteAllBytes(
                    cropDebugPath,
                    cropDebug.EncodeToPNG()
                );

                Destroy(cropDebug);

                Debug.Log($"[SCAN DEBUG] Crop boundary saved: {cropDebugPath}");
            }

            Debug.Log(
                $"[SCAN DEBUG] Frame={rotatedFullFrame.width}x{rotatedFullFrame.height}, " +
                $"crop=({cropX},{cropY}) size={cropSize}"
            );

            Texture2D cropTexture = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
            cropTexture.SetPixels(rotatedFullFrame.GetPixels(cropX, cropY, cropSize, cropSize));
            cropTexture.Apply();

            resizeRT = RenderTexture.GetTemporary(MODEL_INPUT_RESOLUTION, MODEL_INPUT_RESOLUTION, 24);
            Graphics.Blit(cropTexture, resizeRT);
            RenderTexture.active = resizeRT;
            reusableTexture.ReadPixels(new Rect(0, 0, MODEL_INPUT_RESOLUTION, MODEL_INPUT_RESOLUTION), 0, 0);
            reusableTexture.Apply();
            RenderTexture.active = previousActive;

            Destroy(cropTexture);
            RenderTexture.ReleaseTemporary(resizeRT);
            resizeRT = null;

            // Save the exact 224x224 image that will be passed to the model.
            if (saveDebugImages)
            {
                string folderPath = Path.Combine(Application.persistentDataPath, "Compared");
                Directory.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, "predicted_square_image.png");
                File.WriteAllBytes(filePath, reusableTexture.EncodeToPNG());
                Debug.Log($"Saved preprocessed image to: {filePath}");
            }

            Predict(reusableTexture);
        }
        catch (Exception e)
        {
            Debug.LogError($"AI scan failed: {e}");
            DisplayPrediction("AI scan failed. Please try again.", Color.red);
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (tempCamRT != null) RenderTexture.ReleaseTemporary(tempCamRT);
            if (resizeRT != null) RenderTexture.ReleaseTemporary(resizeRT);
            if (fullFrameCaptured != null) Destroy(fullFrameCaptured);
            if (rotatedFullFrame != null) Destroy(rotatedFullFrame);
            isProcessing = false;
        }
    }
    private Texture2D CreateCropDebugImage(
        Texture2D source,
        int cropX,
        int cropY,
        int cropSize
    )
    {
        Texture2D debugImage = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGB24,
            false
        );

        debugImage.SetPixels(source.GetPixels());

        // Draw a red border around the exact crop area.
        int thickness = Mathf.Max(2, source.width / 300);

        for (int t = 0; t < thickness; t++)
        {
            int left = cropX + t;
            int right = cropX + cropSize - 1 - t;
            int bottom = cropY + t;
            int top = cropY + cropSize - 1 - t;

            for (int x = left; x <= right; x++)
            {
                debugImage.SetPixel(x, bottom, Color.red);
                debugImage.SetPixel(x, top, Color.red);
            }

            for (int y = bottom; y <= top; y++)
            {
                debugImage.SetPixel(left, y, Color.red);
                debugImage.SetPixel(right, y, Color.red);
            }
        }

        debugImage.Apply();
        return debugImage;
    }
    // ================================================================
    // BARRACUDA PREDICTION
    // ================================================================

    void Predict(Texture2D image)
    {
        if (worker == null)
        {
            Debug.LogError("Barracuda worker is not initialized.");
            DisplayPrediction("AI Error: Worker not initialized.", Color.red);
            return;
        }

        Color[] pixels = image.GetPixels();
        float[] inputData = new float[MODEL_INPUT_RESOLUTION * MODEL_INPUT_RESOLUTION * 3];

        // Match the Python reference exactly: RGB, scale is already 0..1,
        // then ImageNet mean/std normalization. Barracuda image tensors use NHWC layout.
        for (int i = 0; i < pixels.Length; i++)
        {
            inputData[i * 3]     = (pixels[i].r - INPUT_MEAN[0]) / INPUT_STD[0];
            inputData[i * 3 + 1] = (pixels[i].g - INPUT_MEAN[1]) / INPUT_STD[1];
            inputData[i * 3 + 2] = (pixels[i].b - INPUT_MEAN[2]) / INPUT_STD[2];
        }

        using (Tensor input = new Tensor(1, MODEL_INPUT_RESOLUTION, MODEL_INPUT_RESOLUTION, 3, inputData))
        {
            worker.Execute(input);

            Tensor output = worker.PeekOutput();
            if (output == null)
            {
                Debug.LogError("Barracuda returned no model output.");
                DisplayPrediction("AI Error: No model output.", Color.red);
                return;
            }

            float[] logits;
            try
            {
                logits = output.ToReadOnlyArray();
            }
            finally
            {
                output.Dispose();
            }

            if (logits == null || logits.Length != classLabels.Length)
            {
                Debug.LogError($"Expected {classLabels.Length} model outputs but received {(logits == null ? 0 : logits.Length)}.");
                DisplayPrediction("AI Error: Unexpected model output.", Color.red);
                return;
            }

            // The Python baseline applies Softmax to model logits before evaluating confidence.
            float[] probabilities = Softmax(logits);
            int bestIndex = -1;
            int secondIndex = -1;
            float bestProbability = float.NegativeInfinity;
            float secondProbability = float.NegativeInfinity;

            for (int i = 0; i < probabilities.Length; i++)
            {
                if (probabilities[i] > bestProbability)
                {
                    secondProbability = bestProbability;
                    secondIndex = bestIndex;
                    bestProbability = probabilities[i];
                    bestIndex = i;
                }
                else if (probabilities[i] > secondProbability)
                {
                    secondProbability = probabilities[i];
                    secondIndex = i;
                }
            }

            float margin = bestProbability - (secondIndex >= 0 ? secondProbability : 0f);
            float confidencePercent = bestProbability * 100f;
            float marginPercent = margin * 100f;

            Debug.Log(
                $"[SCAN DEBUG] Worker={selectedWorkerType}\n" +
                $"Input texture={image.width}x{image.height}\n" +
                $"Class order=[{string.Join(", ", classLabels)}]\n" +
                $"Logits=[{string.Join(", ", logits)}]\n" +
                $"Probabilities=[{string.Join(", ", probabilities)}]"
            );
            bool confidentEnough = bestIndex >= 0 &&
                                   bestProbability >= CONFIDENCE_THRESHOLD &&
                                   margin >= MARGIN_THRESHOLD;

            if (!confidentEnough)
            {
                DisplayPrediction($"Unknown\nConfidence: {confidencePercent:F2}%\nMargin: {marginPercent:F2}%", Color.yellow);
                SavePredictionLabel("Unknown");
                Debug.LogWarning($"Prediction rejected as Unknown. Confidence={confidencePercent:P2}, margin={marginPercent:P2}.");

                // The scanning UI is never left, so the player can simply try again.
                // The "Unknown" label is still persisted so a later scene can never
                // pick up a stale prediction.
                DynamicPopupToast.Instance?.ShowToast(
                    "Hmm, I can't tell what that is. Try holding it closer and scan again!",
                    3
                );
                return;
            }

            string rawLabel = classLabels[bestIndex];
            string category = categoryLabels[bestIndex];
            string label = displayLabels[bestIndex];
            Color textColor = confidencePercent >= 90f ? Color.green : Color.white;

            DisplayPrediction($"Predicted: {label}\nConfidence: {confidencePercent:F2}%", textColor);
            Debug.Log($"Predicted: {rawLabel} ({category}), confidence={confidencePercent:F2}%, margin={marginPercent:F2}%");

            SavePredictionLabel(label);

            string statsUsername = GetCurrentUsername();
            if (!string.IsNullOrWhiteSpace(statsUsername))
            {
                FBLeaderboard.Instance?.AddScanStats(
                    statsUsername,
                    category,
                    null,
                    error => Debug.LogError($"Failed to record scan stats: {error}"),
                    label
                );
            }

            if (scanMode == ScanMode.DailyQuest)
            { 
                DailyQuestManager questManager = FindFirstObjectByType<DailyQuestManager>();
                questManager?.AddProgressForScan(rawLabel, category, label, confidencePercent);

                // Plain toast instead of a modal, so scanning stays uninterrupted.
                DynamicPopupToast.Instance?.ShowToast($"You scanned: {label}!", 3);
            }
            else
            {
                sceneHandler?.LoadThrowingInstructionsScene();
            }
        }
    }

    private void SavePredictionLabel(string label)
    {
        try
        {
            string predictionFolder = Path.Combine(Application.persistentDataPath, "Prediction");
            Directory.CreateDirectory(predictionFolder);
            string predictionFilePath = Path.Combine(predictionFolder, "prediction.txt");
            File.WriteAllText(predictionFilePath, label);
            Debug.Log($"Prediction saved to: {predictionFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save prediction: {e.Message}");
        }
    }

    // ================================================================
    // CURRENT USERNAME (for Firebase stats)
    // ================================================================

    private string GetCurrentUsername()
    {
        Dictionary<string, object> profile =
            FBAuthentication.Instance != null
                ? FBAuthentication.Instance.CurrentProfile
                : null;

        if (profile == null && DataManager.Instance != null)
            profile = DataManager.Instance.GetProfile();

        return FirebaseDataHelper.GetString(profile, "username");
    }

    // ================================================================
    // DISPLAY PREDICTION
    // ================================================================

    private void DisplayPrediction(
        string message,
        Color color
    )
    {
        if (predictionText != null)
        {
            predictionText.text = message;
            predictionText.color = color;
        }
        else
        {
            Debug.Log(message);
        }
    }

    // ================================================================
    // DISABLE
    // ================================================================

    void OnDisable()
    {
        if (workerTypeDropdown != null)
            workerTypeDropdown.onValueChanged.RemoveListener(OnWorkerDropdownChanged);
        StopCamera();
    }

    // ================================================================
    // APPLICATION PAUSE / RESUME
    // ================================================================

    void OnApplicationPause(bool pauseStatus)
    {
        if (webCamTexture != null)
        {
            if (pauseStatus)
            {
                if (webCamTexture.isPlaying)
                {
                    webCamTexture.Pause();

                    isCameraReady = false;
                }
            }
            else
            {
                if (!webCamTexture.isPlaying)
                {
                    webCamTexture.Play();

                    StartCoroutine(
                        CheckCameraReadinessAfterResume()
                    );
                }
            }
        }
    }

    // ================================================================
    // CHECK CAMERA AFTER RESUME
    // ================================================================

    IEnumerator CheckCameraReadinessAfterResume()
    {
        yield return new WaitForSeconds(.5f);

        if (webCamTexture != null &&
            webCamTexture.isPlaying &&
            webCamTexture.width > 16)
        {
            isCameraReady = true;

            DisplayPrediction(
                "Camera resumed!",
                Color.green
            );
        }
        else
        {
            DisplayPrediction(
                "Camera resume failed or still initializing.",
                Color.red
            );
        }
    }

    // ================================================================
    // DESTROY
    // ================================================================

    void OnDestroy()
    {
        worker?.Dispose();
        worker = null;

        if (reusableTexture != null)
        {
            Destroy(reusableTexture);
            reusableTexture = null;
        }

        if (scanSquareSprite != null)
        {
            Destroy(scanSquareSprite);
            scanSquareSprite = null;
        }

        if (scanSquareTexture != null)
        {
            Destroy(scanSquareTexture);
            scanSquareTexture = null;
        }

        StopCamera();
    }

    // ================================================================
    // ROTATE TEXTURE 90° CLOCKWISE
    // ================================================================

    private Texture2D RotateTexture90Clockwise(
        Texture2D originalTexture
    )
    {
        int originalWidth =
            originalTexture.width;

        int originalHeight =
            originalTexture.height;

        int newWidth =
            originalHeight;

        int newHeight =
            originalWidth;

        Texture2D rotatedTexture =
            new Texture2D(
                newWidth,
                newHeight,
                originalTexture.format,
                false
            );

        Color[] originalPixels =
            originalTexture.GetPixels();

        Color[] rotatedPixels =
            new Color[
                newWidth * newHeight
            ];

        for (
            int y = 0;
            y < originalHeight;
            y++
        )
        {
            for (
                int x = 0;
                x < originalWidth;
                x++
            )
            {
                int originalIndex =
                    y * originalWidth + x;

                int newX = y;

                int newY =
                    (originalWidth - 1) - x;

                int newIndex =
                    newY * newWidth + newX;

                rotatedPixels[newIndex] =
                    originalPixels[originalIndex];
            }
        }

        rotatedTexture.SetPixels(
            rotatedPixels
        );

        rotatedTexture.Apply();

        return rotatedTexture;
    }

    // ================================================================
    // ORIENTATION CORRECTION FOR THE MODEL INPUT
    //
    // The viewfinder shows the raw camera frame rotated by
    // RawImage.localEulerAngles.z = -videoRotationAngle (and flipped when the
    // camera is vertically mirrored). The AI input has to use the same
    // transform, otherwise the model classifies a rotated copy of what the
    // player is framing.
    // ================================================================

    private Texture2D BuildFrameMatchingPreview(Texture2D rawFrame)
    {
        // Match RawImage.localEulerAngles.z = -videoRotationAngle, which is a
        // counter-clockwise turn, so feed the helper (360 - angle).
        int rotationAngle = webCamTexture != null
            ? Mathf.RoundToInt(webCamTexture.videoRotationAngle)
            : 0;

        bool mirrored = webCamTexture != null && webCamTexture.videoVerticallyMirrored;

        // No orientation metadata (e.g. a desktop webcam): keep the historic
        // 90 degree clockwise turn so Windows behaviour does not change.
        if (rotationAngle == 0 && !mirrored)
        {
            return RotateTexture90Clockwise(rawFrame);
        }

        Texture2D oriented = RotateTextureBy(rawFrame, 360 - rotationAngle);

        // RawImage applies uvRect (0,1,1,-1) when the camera is vertically
        // mirrored, so mirror the pixels to keep the AI input identical.
        return mirrored ? MirrorTextureVertically(oriented) : oriented;
    }

    // Rotates a texture clockwise by a multiple of 90 degrees. Indices use the
    // same bottom-up pixel order as SetPixels/GetPixels, so "clockwise" here is
    // the same visual direction as RawImage.localEulerAngles.z = -angle.
    private Texture2D RotateTextureBy(Texture2D source, int angle)
    {
        int normalizedAngle = ((angle % 360) + 360) % 360;

        if (normalizedAngle == 0)
        {
            return source;
        }

        int width = source.width;
        int height = source.height;

        bool swapsAxes = normalizedAngle == 90 || normalizedAngle == 270;
        int resultWidth = swapsAxes ? height : width;
        int resultHeight = swapsAxes ? width : height;

        Texture2D result = new Texture2D(
            resultWidth,
            resultHeight,
            source.format,
            false
        );

        Color[] sourcePixels = source.GetPixels();
        Color[] resultPixels = new Color[resultWidth * resultHeight];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int destinationIndex;

                switch (normalizedAngle)
                {
                    case 90:
                        destinationIndex = y + x * resultWidth;
                        break;
                    case 180:
                        destinationIndex = (width - 1 - x) + (height - 1 - y) * resultWidth;
                        break;
                    default: // 270
                        destinationIndex = (height - 1 - y) + (width - 1 - x) * resultWidth;
                        break;
                }

                resultPixels[destinationIndex] = sourcePixels[y * width + x];
            }
        }

        result.SetPixels(resultPixels);
        result.Apply();

        return result;
    }

    private Texture2D MirrorTextureVertically(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = texture.GetPixels();

        for (int y = 0; y < height / 2; y++)
        {
            int topRow = y * width;
            int bottomRow = (height - 1 - y) * width;

            for (int x = 0; x < width; x++)
            {
                Color temp = pixels[topRow + x];
                pixels[topRow + x] = pixels[bottomRow + x];
                pixels[bottomRow + x] = temp;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    // ================================================================
    // STOP CAMERA
    // ================================================================

    public void StopCamera()
    {
        if (webCamTexture != null &&
            webCamTexture.isPlaying)
        {
            webCamTexture.Stop();

            isCameraReady = false;
        }
    }

    // ================================================================
    // ODIN DEBUG BUTTONS
    // ================================================================

    [FoldoutGroup("Debug Actions")]
    [Button("Log Scan Diagnostics")]
    private void DebugLogScanDiagnostics()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();

        report.AppendLine("=== SCAN DIAGNOSTICS ===");
        report.AppendLine($"Scan mode            : {scanMode}");
        report.AppendLine($"AI initialised       : {isAiInitialized}");
        report.AppendLine($"Worker               : {(worker == null ? "none" : selectedWorkerType.ToString())}");
        report.AppendLine($"Model asset          : {(modelAsset == null ? "none" : modelAsset.name)}");
        report.AppendLine($"Camera ready         : {isCameraReady}");
        report.AppendLine($"Currently processing : {isProcessing}");

        if (webCamTexture != null)
        {
            report.AppendLine($"Camera texture       : {webCamTexture.width}x{webCamTexture.height} (playing={webCamTexture.isPlaying})");
            report.AppendLine($"Rotation angle       : {webCamTexture.videoRotationAngle}");
            report.AppendLine($"Vertically mirrored  : {webCamTexture.videoVerticallyMirrored}");
        }
        else
        {
            report.AppendLine("Camera texture       : null");
        }

        report.AppendLine($"Crop fraction        : {CENTER_CROP_FRACTION}");
        report.AppendLine($"Confidence threshold : {CONFIDENCE_THRESHOLD:P0}");
        report.AppendLine($"Margin threshold     : {MARGIN_THRESHOLD:P0}");
        report.AppendLine($"Scan square shown    : {showSquare}");
        report.AppendLine($"Debug PNGs enabled   : {saveFullFrameForDebug}");
        report.AppendLine($"Class labels         : {string.Join(", ", classLabels)}");

        string predictionPath = Path.Combine(Application.persistentDataPath, "Prediction", "prediction.txt");
        string savedPrediction = File.Exists(predictionPath)
            ? File.ReadAllText(predictionPath).Trim()
            : "(none)";

        report.AppendLine($"Last saved prediction: {savedPrediction}");
        report.AppendLine($"Debug image folder   : {Path.Combine(Application.persistentDataPath, "Compared")}");

        Debug.Log(report.ToString());
    }

    [FoldoutGroup("Debug Actions")]
    [Button("Force Scan")]
    private void DebugForceScan()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Debug] Enter Play Mode before forcing a scan.");
            return;
        }

        if (!isCameraReady || webCamTexture == null || reusableTexture == null)
        {
            Debug.LogWarning("[Debug] Camera or AI is not ready yet; scan ignored.");
            return;
        }

        Debug.Log("[Debug] Forcing a scan.");
        ProcessFrameForAI();
    }

    [FoldoutGroup("Debug Actions")]
    [Button("Reset Worker To Default")]
    private void DebugResetWorker()
    {
        if (runtimeModel == null)
        {
            Debug.LogWarning("[Debug] Model is not loaded yet; cannot reset the worker.");
            return;
        }

        try
        {
            CreateWorker(WorkerFactory.Type.CSharpBurst);
            RestoreWorkerDropdownSelection();
            Debug.Log($"[Debug] Worker reset to {selectedWorkerType}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Debug] Could not reset worker: {e}");
        }
    }

    [FoldoutGroup("Debug Actions")]
    [Button("Clear Debug Files")]
    private void DebugClearDebugFiles()
    {
        if (isProcessing)
        {
            Debug.LogWarning("[Debug] Cannot clear debug files while a scan is running.");
            return;
        }

        try
        {
            string comparedFolder = Path.Combine(Application.persistentDataPath, "Compared");

            if (Directory.Exists(comparedFolder))
            {
                string[] images = Directory.GetFiles(comparedFolder, "*.png");

                for (int i = 0; i < images.Length; i++)
                    File.Delete(images[i]);

                Debug.Log($"[Debug] Deleted {images.Length} debug image(s) from {comparedFolder}.");
            }
            else
            {
                Debug.Log("[Debug] No debug image folder to clear.");
            }

            string predictionFile = Path.Combine(
                Application.persistentDataPath,
                "Prediction",
                "prediction.txt"
            );

            if (File.Exists(predictionFile))
            {
                File.Delete(predictionFile);
                Debug.Log("[Debug] Cleared prediction.txt.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Debug] Failed to clear debug files: {e}");
        }
    }
}
