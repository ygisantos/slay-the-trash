using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Barracuda;

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

    [Header("UI")]
    public RawImage cameraFeedDisplay;
    public Text predictionText;
    public Button captureButton;

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

    // Model output order (updated model)
    private readonly string[] classLabels =
    {
        "can",
        "glass",
        "organic",
        "other",
        "paper",
        "plastic_bottle",
        "plastic_other"
    };

    // Maps each raw model class (by index) to one of the 3 bin categories
    [Header("Label Mapping (edit when swapping models)")]
    private string[] categoryLabels =
    {
        "recyclable",  // can
        "recyclable",  // glass
        "biological",  // organic
        "non-bio",     // other
        "recyclable",  // paper
        "recyclable",  // plastic_bottle
        "recyclable"   // plastic_other
    };

    // Maps each raw model class (by index) to one of the 5 display categories
    private string[] displayLabels =
    {
        "metal",       // can
        "glass",       // glass
        "food waste",  // organic
        "trash",       // other
        "paper",       // paper
        "plastic",     // plastic_bottle
        "plastic"      // plastic_other
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
            StartCamera();
        }
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
            worker = WorkerFactory.CreateWorker(
                WorkerFactory.Type.ComputePrecompiled,
                runtimeModel
            );

            Debug.Log("Barracuda model loaded successfully.");

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
            captureButton.onClick.RemoveAllListeners();
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

        float cameraSizeMult =
            1600f / webCamTexture.width;

        Debug.Log(cameraSizeMult);

        cameraFeedDisplay.rectTransform.sizeDelta =
            new Vector2(
                webCamTexture.width,
                webCamTexture.height
            ) * cameraSizeMult;

        // Rotate 180° CCW and mirror horizontally
        // cameraFeedDisplay.rectTransform.localEulerAngles =
        //     new Vector3(0, 180, 180);
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

        SoundManager.PlaySound(SoundType.CAMERA);
    }

    // ================================================================
    // CAPTURE + PREPROCESS IMAGE
    // ================================================================

    IEnumerator RunAIPrediction()
    {
        yield return new WaitForSeconds(.5f);

        DisplayPrediction(
            "Processing...",
            Color.yellow
        );

        // ============================================================
        // CAPTURE CAMERA FRAME
        // ============================================================

        RenderTexture tempCamRT =
            RenderTexture.GetTemporary(
                webCamTexture.width,
                webCamTexture.height,
                24
            );

        Graphics.Blit(
            webCamTexture,
            tempCamRT
        );

        RenderTexture.active = tempCamRT;

        Texture2D fullFrameCaptured =
            new Texture2D(
                tempCamRT.width,
                tempCamRT.height,
                TextureFormat.RGB24,
                false
            );

        fullFrameCaptured.ReadPixels(
            new Rect(
                0,
                0,
                tempCamRT.width,
                tempCamRT.height
            ),
            0,
            0
        );

        fullFrameCaptured.Apply();

        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(tempCamRT);

        // ============================================================
        // ROTATE CAMERA IMAGE
        // ============================================================

        Texture2D rotatedFullFrame =
            RotateTexture90Clockwise(
                fullFrameCaptured
            );

        Destroy(fullFrameCaptured);

        // ============================================================
        // RESIZE TO 224x224
        // ============================================================

        RenderTexture resizeRT =
            RenderTexture.GetTemporary(
                MODEL_INPUT_RESOLUTION,
                MODEL_INPUT_RESOLUTION,
                24
            );

        Graphics.Blit(
            rotatedFullFrame,
            resizeRT
        );

        RenderTexture.active = resizeRT;

        reusableTexture.ReadPixels(
            new Rect(
                0,
                0,
                MODEL_INPUT_RESOLUTION,
                MODEL_INPUT_RESOLUTION
            ),
            0,
            0
        );

        reusableTexture.Apply();

        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(resizeRT);

        // ============================================================
        // SAVE RESIZED IMAGE
        // ============================================================

        string folderPath =
            Path.Combine(
                Application.persistentDataPath,
                "Compared"
            );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath =
            Path.Combine(
                folderPath,
                "predicted_square_image.png"
            );

        try
        {
            byte[] pngBytes =
                reusableTexture.EncodeToPNG();

            File.WriteAllBytes(
                filePath,
                pngBytes
            );

            Debug.Log(
                $"Saved resized image for prediction to: {filePath}"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"Failed to save resized image: {e.Message}"
            );

            DisplayPrediction(
                "Error saving resized image.",
                Color.red
            );
        }

        // ============================================================
        // RUN BARRACUDA PREDICTION
        // ============================================================

        Predict(reusableTexture);

        Destroy(rotatedFullFrame);

        isProcessing = false;

        yield return null;
    }

    // ================================================================
    // BARRACUDA PREDICTION
    // ================================================================

    void Predict(Texture2D image)
    {
        if (worker == null)
        {
            Debug.LogError("Barracuda worker is not initialized.");

            DisplayPrediction(
                "AI Error: Worker not initialized.",
                Color.red
            );

            return;
        }

        // ============================================================
        // PREPARE INPUT
        // ============================================================

        Color[] pixels = image.GetPixels();

        float[] inputData =
            new float[
                MODEL_INPUT_RESOLUTION *
                MODEL_INPUT_RESOLUTION *
                3
            ];

        for (int i = 0; i < pixels.Length; i++)
        {
            // Keep your original normalization:
            // 0..1 -> -1..1

            inputData[i * 3 + 0] =
                (pixels[i].r * 2f) - 1f;

            inputData[i * 3 + 1] =
                (pixels[i].g * 2f) - 1f;

            inputData[i * 3 + 2] =
                (pixels[i].b * 2f) - 1f;
        }

        // ============================================================
        // CREATE BARRACUDA INPUT TENSOR
        // ============================================================

        using Tensor input =
            new Tensor(
                1,
                MODEL_INPUT_RESOLUTION,
                MODEL_INPUT_RESOLUTION,
                3,
                inputData
            );

        // ============================================================
        // RUN MODEL
        // ============================================================

        worker.Execute(input);

        // ============================================================
        // GET OUTPUT
        // ============================================================

        Tensor output =
            worker.PeekOutput();

        if (output == null)
        {
            Debug.LogError(
                "Barracuda returned no output."
            );

            DisplayPrediction(
                "AI Error: No model output.",
                Color.red
            );

            return;
        }

        // Download the result to CPU memory.
        float[] outputData =
            output.ToReadOnlyArray();

        output.Dispose();

        // ============================================================
        // FIND HIGHEST CLASS
        // ============================================================

        float maxVal = float.MinValue;
        int predictedIndex = -1;

        for (int i = 0; i < outputData.Length; i++)
        {
            if (outputData[i] > maxVal)
            {
                maxVal = outputData[i];
                predictedIndex = i;
            }
        }

        // ============================================================
        // LABEL
        // ============================================================

        string rawLabel =
            predictedIndex >= 0 &&
            predictedIndex < classLabels.Length
                ? classLabels[predictedIndex]
                : "Unknown";

        string category =
            predictedIndex >= 0 &&
            predictedIndex < categoryLabels.Length
                ? categoryLabels[predictedIndex]
                : "Unknown";

        string label =
            predictedIndex >= 0 &&
            predictedIndex < displayLabels.Length
                ? displayLabels[predictedIndex]
                : "Unknown";

        // ============================================================
        // CONFIDENCE
        // ============================================================

        float confidencePercent =
            maxVal * 100f;

        // IMPORTANT:
        // This assumes your ONNX output is already probabilities
        // between 0 and 1, which matches how your old code worked.
        //
        // If your model outputs logits instead, we should apply
        // Softmax instead.

        // ============================================================
        // DISPLAY COLOR
        // ============================================================

        Color textColor =
            confidencePercent >= 90f
                ? Color.green
                : Color.red;

        // ============================================================
        // DISPLAY RESULT
        // ============================================================

        string displayMessage =
            $"Predicted: {label}\n" +
            $"Confidence: {confidencePercent:F2}%";

        DisplayPrediction(
            displayMessage,
            textColor
        );

        Debug.Log(
            $"Predicted: {rawLabel} ({category})\n" +
            $"Confidence: {confidencePercent:F2}%"
        );

        // ============================================================
        // SAVE PREDICTION
        // ============================================================

        string predictionFolder =
            Path.Combine(
                Application.persistentDataPath,
                "Prediction"
            );

        if (!Directory.Exists(predictionFolder))
        {
            Directory.CreateDirectory(
                predictionFolder
            );
        }

        string predictionFilePath =
            Path.Combine(
                predictionFolder,
                "prediction.txt"
            );

        try
        {
            File.WriteAllText(
                predictionFilePath,
                label
            );

            Debug.Log(
                $"Prediction saved to: {predictionFilePath}"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"Failed to save prediction: {e.Message}"
            );
        }

        // ============================================================
        // RECORD FIREBASE STATS (total/non-bio/bio/recyclable counts)
        // Applies to every scan regardless of Collection vs DailyQuest mode.
        // ============================================================

        string statsUsername = GetCurrentUsername();
        if (!string.IsNullOrWhiteSpace(statsUsername))
        {
            FBLeaderboard.Instance?.AddScanStats(
                statsUsername,
                category,
                null,
                error => Debug.LogError($"Failed to record scan stats: {error}")
            );
        }

        // ============================================================
        // CONTINUE BASED ON SCAN MODE
        // ============================================================

        if (scanMode == ScanMode.DailyQuest)
        {
            // Stay in the modal; update quest progress and show the result directly.
            DailyQuestManager questManager = FindFirstObjectByType<DailyQuestManager>();
            questManager?.AddProgressForScan(rawLabel, category, label, confidencePercent);

            DialogueManager.Instance?.ShowScanResult($"You scanned: {label}!");
        }
        else
        {
            // Legacy "add new collection" flow, unchanged.
            sceneHandler?.LoadThrowingInstructionsScene();
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
}
