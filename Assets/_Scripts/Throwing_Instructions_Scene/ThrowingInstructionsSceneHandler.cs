using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ThrowingInstructionsSceneHandler: MonoBehaviour
{
    public void LoadTrashScannerScene()
    {
        Transitioner.Instance.TransitionToScene("AIScannerScene");
    }
    
    public void LoadOCRSceneScene()
    {
        Transitioner.Instance.TransitionToScene("OCRScene");
    }

    public void LoadMainMenuScene()
    {
        Transitioner.Instance.TransitionToScene("MainMenuScene");
    }

    public void LoadScene(string sceneName)
    {
        Transitioner.Instance.TransitionToScene(sceneName);
    }
}
