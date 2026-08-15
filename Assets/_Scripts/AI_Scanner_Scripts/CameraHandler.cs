using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Barracuda;
using System.IO;

public class CameraHandler : MonoBehaviour
{
    public RawImage cameraFeedDisplay;
    public NNModel modelAsset;
    public Text predictionText;
    public Button captureButton;
    public AIScannerSceneHandlerScript sceneHandler;

    private WebCamTexture webCamTexture;
    private bool isCameraReady = false;
    private Model model;
    private IWorker worker;
    private string[] classLabels = { "food waste", "paper", "plastic bottle" };

    private Texture2D reusableTexture;
    private bool isProcessing = false;

    private const int MODEL_INPUT_RESOLUTION = 224;

    void Start()
    {
        StartCoroutine(InitializeCameraAndAI());
    }

    IEnumerator InitializeCameraAndAI()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam) || WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("No camera devices found or camera permission denied.");
            DisplayPrediction("Camera Error: No device or permission.", Color.red);
            yield break;
        }

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
        webCamTexture.Play();

        while (webCamTexture.width <= 16 || webCamTexture.height <= 16)
        {
            DisplayPrediction("Initializing Camera...", Color.yellow);
            yield return null;
        }

        isCameraReady = true;
        ApplyCameraFeedDisplaySettings();

        if (modelAsset == null)
        {
            Debug.LogError("NNModel asset is not assigned in the Inspector!");
            DisplayPrediction("AI Error: Model not assigned.", Color.red);
            yield break;
        }

        model = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        DisplayPrediction("Camera & AI Ready!", Color.green);

        if (captureButton != null)
        {
            captureButton.onClick.RemoveAllListeners();
            captureButton.onClick.AddListener(ProcessFrameForAI);
        }

        reusableTexture = new Texture2D(MODEL_INPUT_RESOLUTION, MODEL_INPUT_RESOLUTION, TextureFormat.RGB24, false);
    }

    void ApplyCameraFeedDisplaySettings()
    {
        if (webCamTexture == null || cameraFeedDisplay == null) return;

        //float camerRatio = (float)webCamTexture.width/webCamTexture.height;
        //Debug.Log((float)webCamTexture.width / webCamTexture.height);
        //if (camerRatio >= (float)Screen.width/Screen.height)
        //    cameraFeedDisplay.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        //else// Match display size to raw webcam resolution
        //    cameraFeedDisplay.rectTransform.sizeDelta = new Vector2(webCamTexture.width, webCamTexture.height);
        Debug.Log($"{webCamTexture.width} {webCamTexture.height}");
        float cameraSizeMult = 1600f / webCamTexture.width;
        Debug.Log(cameraSizeMult);
        cameraFeedDisplay.rectTransform.sizeDelta = new Vector2(webCamTexture.width, webCamTexture.height) * cameraSizeMult;

        // Rotate 180° CCW and mirror horizontally
        cameraFeedDisplay.rectTransform.localEulerAngles = new Vector3(0, 180, 180);
    }

    public void ProcessFrameForAI()
    {
        if (!isCameraReady || webCamTexture == null || !webCamTexture.isPlaying || isProcessing)
        {
            return;
        }

        StartCoroutine(RunAIPrediction());
        SoundManager.PlaySound(SoundType.CAMERA);
    }

    IEnumerator RunAIPrediction()
    {
        yield return new WaitForSeconds(.5f);
        isProcessing = true;
        DisplayPrediction("Processing...", Color.yellow);

        RenderTexture tempCamRT = RenderTexture.GetTemporary(webCamTexture.width, webCamTexture.height, 24);
        Graphics.Blit(webCamTexture, tempCamRT);

        RenderTexture.active = tempCamRT;
        Texture2D fullFrameCaptured = new Texture2D(tempCamRT.width, tempCamRT.height, TextureFormat.RGB24, false);
        fullFrameCaptured.ReadPixels(new Rect(0, 0, tempCamRT.width, tempCamRT.height), 0, 0);
        fullFrameCaptured.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempCamRT);

        Texture2D rotatedFullFrame = RotateTexture90Clockwise(fullFrameCaptured);
        Destroy(fullFrameCaptured);

        RenderTexture resizeRT = RenderTexture.GetTemporary(MODEL_INPUT_RESOLUTION, MODEL_INPUT_RESOLUTION, 24);
        Graphics.Blit(rotatedFullFrame, resizeRT);

        RenderTexture.active = resizeRT;
        reusableTexture.ReadPixels(new Rect(0, 0, MODEL_INPUT_RESOLUTION, MODEL_INPUT_RESOLUTION), 0, 0);
        reusableTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(resizeRT);

        string folderPath = Path.Combine(Application.persistentDataPath, "Compared");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string filePath = Path.Combine(folderPath, "predicted_square_image.png");
        try
        {
            byte[] pngBytes = reusableTexture.EncodeToPNG();
            File.WriteAllBytes(filePath, pngBytes);
            Debug.Log($"Saved resized image for prediction to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save resized image: {e.Message}");
            DisplayPrediction("Error saving resized image.", Color.red);
        }

        Predict(reusableTexture);

        Destroy(rotatedFullFrame);

        isProcessing = false;
        yield return null;
    }

    void Predict(Texture2D image)
    {
        Color[] pixels = image.GetPixels();
        float[] inputData = new float[MODEL_INPUT_RESOLUTION * MODEL_INPUT_RESOLUTION * 3];

        for (int i = 0; i < pixels.Length; i++)
        {
            inputData[i * 3 + 0] = (pixels[i].r * 2f) - 1f;
            inputData[i * 3 + 1] = (pixels[i].g * 2f) - 1f;
            inputData[i * 3 + 2] = (pixels[i].b * 2f) - 1f;
        }

        Tensor input = new Tensor(1, MODEL_INPUT_RESOLUTION, MODEL_INPUT_RESOLUTION, 3, inputData);

        worker.Execute(input);
        Tensor output = worker.PeekOutput();

        float maxVal = -1f;
        int predictedIndex = -1;

        for (int i = 0; i < output.length; i++)
        {
            if (output[i] > maxVal)
            {
                maxVal = output[i];
                predictedIndex = i;
            }
        }

        string label = predictedIndex >= 0 && predictedIndex < classLabels.Length ? classLabels[predictedIndex] : "Unknown";
        float confidencePercent = maxVal * 100f;
        string displayMessage = $"Predicted: {label}\nConfidence: {confidencePercent:F2}%";
        Color textColor = predictedIndex >= 90 ? Color.green : Color.red;

        DisplayPrediction(displayMessage, textColor);
        Debug.Log(displayMessage);

        string predictionFolder = Path.Combine(Application.persistentDataPath, "Prediction");
        if (!Directory.Exists(predictionFolder))
        {
            Directory.CreateDirectory(predictionFolder);
        }

        string predictionFilePath = Path.Combine(predictionFolder, "prediction.txt");
        try
        {
            File.WriteAllText(predictionFilePath, label);
            Debug.Log($"Prediction saved to: {predictionFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save prediction: {e.Message}");
        }


	sceneHandler?.LoadThrowingInstructionsScene();

        input.Dispose();
        output.Dispose();

    }

    private void DisplayPrediction(string message, Color color)
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

    void OnDisable()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
            isCameraReady = false;
        }
        worker?.Dispose();
    }

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
                    StartCoroutine(CheckCameraReadinessAfterResume());
                }
            }
        }
    }

    IEnumerator CheckCameraReadinessAfterResume()
    {
        yield return new WaitForSeconds(0.5f);
        if (webCamTexture != null && webCamTexture.isPlaying && webCamTexture.width > 16)
        {
            isCameraReady = true;
            DisplayPrediction("Camera resumed!", Color.green);
        }
        else
        {
            DisplayPrediction("Camera resume failed or still initializing.", Color.red);
        }
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    private Texture2D RotateTexture90Clockwise(Texture2D originalTexture)
    {
        int originalWidth = originalTexture.width;
        int originalHeight = originalTexture.height;

        int newWidth = originalHeight;
        int newHeight = originalWidth;

        Texture2D rotatedTexture = new Texture2D(newWidth, newHeight, originalTexture.format, false);

        Color[] originalPixels = originalTexture.GetPixels();
        Color[] rotatedPixels = new Color[newWidth * newHeight];

        for (int y = 0; y < originalHeight; y++)
        {
            for (int x = 0; x < originalWidth; x++)
            {
                int originalIndex = y * originalWidth + x;
                int newX = y;
                int newY = (originalWidth - 1) - x;
                int newIndex = newY * newWidth + newX;
                rotatedPixels[newIndex] = originalPixels[originalIndex];
            }
        }

        rotatedTexture.SetPixels(rotatedPixels);
        rotatedTexture.Apply();

        return rotatedTexture;
    }

    public void StopCamera()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
            isCameraReady = false;
        }
    }
}
