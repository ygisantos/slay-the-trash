using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Barracuda;

public class CameraHandler_copy : MonoBehaviour
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
        "biological",  // paper  ← paper is biodegradable
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
            cameraRoutine = null;
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
                // WorkerFactory.Type.CSharpBurst,
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
        yield return new WaitForSeconds(0.5f);

        DisplayPrediction("Processing...", Color.yellow);

        Texture2D fullFrameCaptured = null;
        Texture2D rotatedFullFrame = null;
        Texture2D croppedTexture = null;
        RenderTexture tempCamRT = null;
        RenderTexture resizeRT = null;

        RenderTexture previousRT = RenderTexture.active;

        try
        {
            // ============================================================
            // CAPTURE CAMERA FRAME
            // ============================================================

            tempCamRT = RenderTexture.GetTemporary(
                webCamTexture.width,
                webCamTexture.height,
                24
            );

            Graphics.Blit(webCamTexture, tempCamRT);

            RenderTexture.active = tempCamRT;

            fullFrameCaptured = new Texture2D(
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

            RenderTexture.active = previousRT;
            RenderTexture.ReleaseTemporary(tempCamRT);
            tempCamRT = null;

            // ============================================================
            // ROTATE CAMERA IMAGE
            // Keep the rotation used by your original implementation.
            // ============================================================

            rotatedFullFrame = RotateTexture90Clockwise(
                fullFrameCaptured
            );

            Destroy(fullFrameCaptured);
            fullFrameCaptured = null;

            // ============================================================
            // CENTERED SQUARE CROP
            // Match Python: 65% of the shorter frame dimension.
            // ============================================================

            int frameWidth = rotatedFullFrame.width;
            int frameHeight = rotatedFullFrame.height;

            int cropSize = Mathf.Max(
                1,
                Mathf.FloorToInt(
                    Mathf.Min(frameWidth, frameHeight) * 0.65f
                )
            );

            int cropX = (frameWidth - cropSize) / 2;
            int cropY = (frameHeight - cropSize) / 2;

            Color[] framePixels = rotatedFullFrame.GetPixels();
            Color[] cropPixels = new Color[cropSize * cropSize];

            for (int y = 0; y < cropSize; y++)
            {
                for (int x = 0; x < cropSize; x++)
                {
                    int sourceIndex =
                        (cropY + y) * frameWidth + (cropX + x);

                    int targetIndex = y * cropSize + x;

                    cropPixels[targetIndex] = framePixels[sourceIndex];
                }
            }

            croppedTexture = new Texture2D(
                cropSize,
                cropSize,
                TextureFormat.RGB24,
                false
            );

            croppedTexture.SetPixels(cropPixels);
            croppedTexture.Apply();

            // ============================================================
            // RESIZE CROPPED IMAGE TO 224 x 224
            // ============================================================

            resizeRT = RenderTexture.GetTemporary(
                MODEL_INPUT_RESOLUTION,
                MODEL_INPUT_RESOLUTION,
                0,
                RenderTextureFormat.ARGB32
            );

            Graphics.Blit(croppedTexture, resizeRT);

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

            RenderTexture.active = previousRT;

            RenderTexture.ReleaseTemporary(resizeRT);
            resizeRT = null;

            // ============================================================
            // SAVE THE EXACT RESIZED IMAGE USED FOR PREDICTION
            // ============================================================

            string folderPath = Path.Combine(
                Application.persistentDataPath,
                "Compared"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(
                folderPath,
                "predicted_square_image.png"
            );

            byte[] pngBytes = reusableTexture.EncodeToPNG();
            File.WriteAllBytes(filePath, pngBytes);

            Debug.Log(
                $"Saved resized image for prediction to: {filePath}"
            );

            // ============================================================
            // RUN PREDICTION
            // ============================================================

            Predict(reusableTexture);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"AI prediction failed: {e}"
            );

            DisplayPrediction(
                "AI Error: Prediction failed.",
                Color.red
            );
        }
        finally
        {
            // ============================================================
            // CLEAN UP RESOURCES
            // ============================================================

            RenderTexture.active = previousRT;

            if (tempCamRT != null)
            {
                RenderTexture.ReleaseTemporary(tempCamRT);
            }

            if (resizeRT != null)
            {
                RenderTexture.ReleaseTemporary(resizeRT);
            }

            if (fullFrameCaptured != null)
            {
                Destroy(fullFrameCaptured);
            }

            if (rotatedFullFrame != null)
            {
                Destroy(rotatedFullFrame);
            }

            if (croppedTexture != null)
            {
                Destroy(croppedTexture);
            }

            isProcessing = false;
        }
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

        if (image == null)
        {
            Debug.LogError("Prediction image is null.");

            DisplayPrediction(
                "AI Error: Image is missing.",
                Color.red
            );

            return;
        }

        // ============================================================
        // PREPARE INPUT USING PYTHON'S IMAGENET NORMALIZATION
        // ============================================================

        Color[] pixels = image.GetPixels();

        float[] inputData = new float[
            MODEL_INPUT_RESOLUTION *
            MODEL_INPUT_RESOLUTION *
            3
        ];

        float[] mean = { 0.485f, 0.456f, 0.406f };
        float[] std  = { 0.229f, 0.224f, 0.225f };

        for (int i = 0; i < pixels.Length; i++)
        {
            // Texture2D.GetPixels() normally returns RGB values
            // normalized to the range 0..1.

            inputData[i * 3 + 0] =
                (pixels[i].r - mean[0]) / std[0];

            inputData[i * 3 + 1] =
                (pixels[i].g - mean[1]) / std[1];

            inputData[i * 3 + 2] =
                (pixels[i].b - mean[2]) / std[2];
        }

        // ============================================================
        // CREATE BARRACUDA INPUT TENSOR
        // ============================================================

        using Tensor input = new Tensor(
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
        // GET RAW MODEL OUTPUT
        // ============================================================

        Tensor output = worker.PeekOutput();

        if (output == null)
        {
            Debug.LogError("Barracuda returned no model output.");

            DisplayPrediction(
                "AI Error: No model output.",
                Color.red
            );

            return;
        }

        float[] outputData;

        try
        {
            outputData = output.ToReadOnlyArray();
        }
        finally
        {
            output.Dispose();
        }

        // This classifier is expected to produce five class scores.
        if (outputData.Length != classLabels.Length)
        {
            Debug.LogError(
                $"Unexpected output size: {outputData.Length}. " +
                $"Expected: {classLabels.Length}."
            );

            DisplayPrediction(
                "AI Error: Unexpected model output.",
                Color.red
            );

            return;
        }

        // ============================================================
        // LOG RAW OUTPUTS FOR PYTHON COMPARISON
        // ============================================================

        Debug.Log(
            "Raw model outputs: " +
            string.Join(", ", outputData)
        );

        // ============================================================
        // SOFTMAX
        // Matches Python if the ONNX model returns logits.
        // ============================================================

        float[] probabilities = Softmax(outputData);

        // ============================================================
        // FIND TOP TWO CLASSES
        // ============================================================

        int predictedIndex = -1;

        float maxVal = float.MinValue;
        float secondMaxVal = float.MinValue;

        for (int i = 0; i < probabilities.Length; i++)
        {
            if (probabilities[i] > maxVal)
            {
                secondMaxVal = maxVal;
                maxVal = probabilities[i];
                predictedIndex = i;
            }
            else if (probabilities[i] > secondMaxVal)
            {
                secondMaxVal = probabilities[i];
            }
        }

        if (predictedIndex < 0)
        {
            DisplayPrediction(
                "AI Error: Invalid prediction.",
                Color.red
            );

            return;
        }

        // ============================================================
        // CONFIDENCE AND MARGIN
        // ============================================================

        float confidencePercent = maxVal * 100f;

        float marginPercent =
            (maxVal - secondMaxVal) * 100f;

        const float CONFIDENCE_THRESHOLD = 80f;
        const float MARGIN_THRESHOLD = 20f;

        bool accepted =
            confidencePercent >= CONFIDENCE_THRESHOLD &&
            marginPercent >= MARGIN_THRESHOLD;

        // ============================================================
        // MAP CLASS LABELS
        // ============================================================

        string rawLabel = classLabels[predictedIndex];

        string category =
            predictedIndex < categoryLabels.Length
                ? categoryLabels[predictedIndex]
                : "Unknown";

        string label =
            predictedIndex < displayLabels.Length
                ? displayLabels[predictedIndex]
                : "Unknown";

        if (!accepted)
        {
            category = "Unknown";
            label = "Unknown";
        }

        // ============================================================
        // DISPLAY RESULT
        // ============================================================

        Color textColor = accepted
            ? Color.green
            : Color.red;

        string displayMessage = accepted
            ? $"Predicted: {label}\n" +
            $"Confidence: {confidencePercent:F2}%"
            : $"Unknown\n" +
            $"Top result: {rawLabel}\n" +
            $"Confidence: {confidencePercent:F2}%";

        DisplayPrediction(
            displayMessage,
            textColor
        );

        Debug.Log(
            $"Top class: {rawLabel}\n" +
            $"Category: {category}\n" +
            $"Display label: {label}\n" +
            $"Confidence: {confidencePercent:F2}%\n" +
            $"Margin: {marginPercent:F2}%\n" +
            $"Accepted: {accepted}"
        );

        // ============================================================
        // SAVE PREDICTION
        // ============================================================

        string predictionFolder = Path.Combine(
            Application.persistentDataPath,
            "Prediction"
        );

        if (!Directory.Exists(predictionFolder))
        {
            Directory.CreateDirectory(predictionFolder);
        }

        string predictionFilePath = Path.Combine(
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
        // REJECT UNCERTAIN PREDICTIONS
        // Do not record stats, advance quests, or change scenes.
        // ============================================================

        if (!accepted)
        {
            Debug.LogWarning(
                "Prediction rejected because confidence or margin " +
                "did not meet the threshold."
            );

            return;
        }

        // ============================================================
        // DAILY QUEST OR COLLECTION FLOW
        // ============================================================

        if (scanMode == ScanMode.DailyQuest)
        {
            string statsUsername = GetCurrentUsername();

            if (!string.IsNullOrWhiteSpace(statsUsername))
            {
                Debug.Log(
                    $"Recording scan stats for '{statsUsername}' " +
                    $"(category: {category}, type: {label})."
                );

                FBLeaderboard.Instance?.AddScanStats(
                    statsUsername,
                    category,
                    () => Debug.Log("Scan stats saved."),
                    error =>
                    {
                        Debug.LogError(
                            $"Failed to record scan stats: {error}"
                        );

                        DynamicPopupToast.Instance?.ShowToast(
                            "Stats not saved: " + error,
                            4
                        );
                    },
                    label
                );
            }
            else
            {
                Debug.LogWarning(
                    "Scan stats skipped: no username found " +
                    "in the current profile."
                );
            }

            DailyQuestManager questManager =
                FindFirstObjectByType<DailyQuestManager>();

            questManager?.AddProgressForScan(
                rawLabel,
                category,
                label,
                confidencePercent
            );

            DialogueManager.Instance?.ShowScanResult(
                $"You scanned: {label}!"
            );
        }
        else
        {
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

    // public void StopCamera()
    // {
    //     if (webCamTexture != null &&
    //         webCamTexture.isPlaying)
    //     {
    //         webCamTexture.Stop();

    //         isCameraReady = false;
    //     }
    // }
    public void StopCamera()
    {
        // Cancel initialization if it is still running.
        if (cameraRoutine != null)
        {
            StopCoroutine(cameraRoutine);
            cameraRoutine = null;
        }

        isCameraReady = false;
        isProcessing = false;

        // Release the webcam and its native resources.
        if (webCamTexture != null)
        {
            if (webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            // Detach it from the UI before destroying it.
            if (cameraFeedDisplay != null &&
                cameraFeedDisplay.texture == webCamTexture)
            {
                cameraFeedDisplay.texture = null;
            }

            Destroy(webCamTexture);
            webCamTexture = null;
        }
    }
}
