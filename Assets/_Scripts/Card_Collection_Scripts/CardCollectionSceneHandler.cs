using UnityEngine;
using UnityEngine.SceneManagement;

public class CardCollectionSceneHandler: MonoBehaviour
{

    public void LoadMainMenuScene()
    {
        Transitioner.Instance.TransitionToScene("MainMenuScene");
    }

    public void LoadScannerScene()
    {
        Debug.Log("AIScanner Loaded");
        Transitioner.Instance.TransitionToScene("AIScannerScene");
    }

    public void LoadScene(string sceneName)
    {
        Transitioner.Instance.TransitionToScene(sceneName);
    }
}
