using UnityEngine;
using UnityEngine.UI;

public class CameraAspect : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;
    [SerializeField] private AspectRatioFitter aspectFitter;
    [SerializeField] private WebCamTexture webcam;

    void Update()
    {
        if (webcam != null && webcam.width > 100)
        {
            aspectFitter.aspectRatio =
                (float)webcam.width / webcam.height;
        }
    }
}