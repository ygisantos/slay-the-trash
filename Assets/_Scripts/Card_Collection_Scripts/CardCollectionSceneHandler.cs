using UnityEngine;
using UnityEngine.SceneManagement;

public class CardCollectionSceneHandler: MonoBehaviour
{

    public void LoadMainMenuScene()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public void LoadScannerScene()
    {
        Debug.Log("AIScanner Loaded");
        SceneManager.LoadScene("AIScannerScene");
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
