// Backup of CameraHandler.cs before switching to the WasteWise 8-class model (SriramRokkam/wastewise-garbage-cls).
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using Unity.Barracuda;

public class CameraHandler_Backup : MonoBehaviour
{
    [Header("UI")]
    public RawImage cameraFeedDisplay;
    public Text predictionText;
    public Button captureButton;

    [Header("AI")]
    public NNModel modelAsset;

    [Header("Scene")]
    public AIScannerSceneHandlerScript sceneHandler;

    private WebCamTexture webCamTexture;
    private bool isCameraReady = false;

    // Barracuda
    private Model runtimeModel;
    private IWorker worker;

    private string[] classLabels =
    {
        "food waste",
        "paper",
        "plastic bottle"
    };

    private Texture2D reusableTexture;
    private bool isProcessing = false;

    private const int MODEL_INPUT_RESOLUTION = 224;

    void Start()
    {
        StartCoroutine(InitializeCameraAndAI());
    }

    IEnumerator InitializeCameraAndAI()
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
        // SELECT CAMERA
        // ============================================================

        WebCamDevice device = WebCamTexture.devices[0];

        foreach (var camDevice in WebCamTexture.devices)
        {
            if (!camDevice.isFrontFacing)
            {
                device = camDevice;
                break;
            }
        }

        // ============================================================
        // START CAMERA
        // ============================================================

        webCamTexture = new WebCamTexture(device.name);

        cameraFeedDisplay.texture = webCamTexture;

        webCamTexture.Play();

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
        cameraFeedDisplay.rectTransform.localEulerAngles =
            new Vector3(0, 180, 180);
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

        StartCoroutine(RunAIPrediction());

        SoundManager.PlaySound(SoundType.CAMERA);
    }

    // ================================================================
    // CAPTURE + PREPROCESS IMAGE
    // ================================================================

    IEnumerator RunAIPrediction()
    {
        yield return new WaitForSeconds(.5f);

        isProcessing = true;

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

        string label =
            predictedIndex >= 0 &&
            predictedIndex < classLabels.Length
                ? classLabels[predictedIndex]
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

        Debug.Log(displayMessage);

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
        // LOAD THROWING INSTRUCTIONS
        // ============================================================

        sceneHandler?.LoadThrowingInstructionsScene();
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
        if (webCamTexture != null &&
            webCamTexture.isPlaying)
        {
            webCamTexture.Stop();

            isCameraReady = false;
        }

        worker?.Dispose();
        worker = null;
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
