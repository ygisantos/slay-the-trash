using UnityEngine;
using UnityEngine.SceneManagement;

public class OCRSceneHandlerScript : MonoBehaviour
{
    public OCRCameraHandler ocrCameraHandler; 

    public void LoadAIScannerScene()
    {
        StopCameraIfRunning();
        Transitioner.Instance.TransitionToScene("AIScannerScene");
    }

    public void LoadBinResultScene()
    {
        StopCameraIfRunning();
        Transitioner.Instance.TransitionToScene("BinResultScene");
    }

    private void StopCameraIfRunning()
    {
        if (ocrCameraHandler != null)
        {
            ocrCameraHandler.StopCamera();
        }
    }
}
