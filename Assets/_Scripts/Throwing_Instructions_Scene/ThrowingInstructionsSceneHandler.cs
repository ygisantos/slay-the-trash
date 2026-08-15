using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ThrowingInstructionsSceneHandler: MonoBehaviour
{
    public void LoadTrashScannerScene()
    {
        SceneManager.LoadScene("AIScannerScene");
    }
    
    public void LoadOCRSceneScene()
    {
        SceneManager.LoadScene("OCRScene");
    }

    public void LoadMainMenuScene()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
