using UnityEngine;
using UnityEngine.SceneManagement;

public class OCRSceneHandlerScript : MonoBehaviour
{
    public OCRCameraHandler ocrCameraHandler; 

    public void LoadAIScannerScene()
    {
        StopCameraIfRunning();
        SceneManager.LoadScene("AIScannerScene");
    }

    public void LoadBinResultScene()
    {
        StopCameraIfRunning();
        SceneManager.LoadScene("BinResultScene");
    }

    private void StopCameraIfRunning()
    {
        if (ocrCameraHandler != null)
        {
            ocrCameraHandler.StopCamera();
        }
    }
}
