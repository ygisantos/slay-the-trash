using UnityEngine;
using UnityEngine.SceneManagement;

public class AIScannerSceneHandlerScript : MonoBehaviour
{
    public CameraHandler cameraHandler; // Drag and drop this in the Inspector

    public void LoadMainMenuScene()
    {
        StopCameraIfRunning();
        Transitioner.Instance.TransitionToScene("MainMenuScene");
    }

    public void LoadThrowingInstructionsScene()
    {
        StopCameraIfRunning();
        Transitioner.Instance.TransitionToScene("ThrowingInstructionsScene");
    }

    public void LoadScene(string sceneName)
    {
        Transitioner.Instance.TransitionToScene(sceneName);
    }

    private void StopCameraIfRunning()
    {
        if (cameraHandler != null)
        {
            cameraHandler.StopCamera();
        }
    }
}
