using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class OCRCameraHandler : MonoBehaviour
{
    public RawImage cameraFeedDisplay;
    public Text predictionText;
    public Button captureButton;

    public OCRSpaceUploader ocrUploader;

    private WebCamTexture webCamTexture;
    private bool isCameraReady = false;

    void Start()
    {
        StartCoroutine(InitializeCamera());

        if (captureButton != null)
        {
            captureButton.onClick.RemoveAllListeners();
            captureButton.onClick.AddListener(SaveCurrentFrameAsJPG);
        }
    }

    IEnumerator InitializeCamera()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam) || WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("No camera devices found or permission denied.");
            DisplayMessage("Camera Error: No device or permission.", Color.red);
            yield break;
        }

        WebCamDevice device = WebCamTexture.devices[0];
        // Prefer non-front facing camera
        foreach (var camDevice in WebCamTexture.devices)
        {
            if (!camDevice.isFrontFacing)
            {
                device = camDevice;
                break;
            }
        }

        // Create WebCamTexture with default resolution (no hardcoded width/height)
        webCamTexture = new WebCamTexture(device.name);
        cameraFeedDisplay.texture = webCamTexture;
        webCamTexture.Play();

        // Wait for camera to initialize (width and height become available)
        while (webCamTexture.width <= 16 || webCamTexture.height <= 16)
        {
            DisplayMessage("Initializing Camera...", Color.yellow);
            yield return null;
        }

        isCameraReady = true;

        // Apply display transform to match CameraHandler
        ApplyCameraFeedDisplaySettings();

        DisplayMessage("Camera Ready!", Color.green);
    }

    void ApplyCameraFeedDisplaySettings()
    {
        if (webCamTexture == null || cameraFeedDisplay == null) return;

        // Set RawImage size to webcam texture's actual resolution
	cameraFeedDisplay.rectTransform.sizeDelta = new Vector2(webCamTexture.width, webCamTexture.height);

        // Rotate 90° CCW and mirror horizontally (same as CameraHandler)
        cameraFeedDisplay.rectTransform.localEulerAngles = new Vector3(0, 180, 90);
    }

    public void SaveCurrentFrameAsJPG()
    {
        if (!isCameraReady || webCamTexture == null || !webCamTexture.isPlaying)
        {
            Debug.LogWarning("Camera not ready or not playing.");
            return;
        }

        Texture2D original = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        original.SetPixels(webCamTexture.GetPixels());
        original.Apply();

        Texture2D rotated = RotateTexture90Clockwise(original);
        MirrorTextureVertically(rotated);
        MirrorTextureHorizontally(rotated);

        byte[] jpgBytes = rotated.EncodeToJPG(90);

        string folderPath = Path.Combine(Application.persistentDataPath, "OCR_image");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "image.jpg");

        try
        {
            File.WriteAllBytes(filePath, jpgBytes);
            Debug.Log($"Saved snapshot to: {filePath}");
            DisplayMessage("Saved to /OCR_image/image.jpg", Color.cyan);

            if (ocrUploader != null)
            {
                StartCoroutine(ocrUploader.UploadImageForOCR(filePath));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save JPG: {e.Message}");
            DisplayMessage("Error saving image.", Color.red);
        }

        Destroy(original);
        Destroy(rotated);
    }

    private Texture2D RotateTexture90Clockwise(Texture2D original)
    {
        int width = original.width;
        int height = original.height;

        Texture2D rotated = new Texture2D(height, width, original.format, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                rotated.SetPixel(height - y - 1, x, original.GetPixel(x, y));
            }
        }

        rotated.Apply();
        return rotated;
    }

    private void MirrorTextureVertically(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = texture.GetPixels();

        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int top = y * width + x;
                int bottom = (height - 1 - y) * width + x;

                Color temp = pixels[top];
                pixels[top] = pixels[bottom];
                pixels[bottom] = temp;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private void MirrorTextureHorizontally(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = texture.GetPixels();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width / 2; x++)
            {
                int left = y * width + x;
                int right = y * width + (width - 1 - x);

                Color temp = pixels[left];
                pixels[left] = pixels[right];
                pixels[right] = temp;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private void DisplayMessage(string message, Color color)
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
    }

    void OnDestroy()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
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
