using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BinResultSceneHandler: MonoBehaviour
{
    public void LoadOCRScene()
    {
        Transitioner.Instance.TransitionToScene("OCRScene");
    }
    
    public void LoadMainMenuScene()
    {
        Transitioner.Instance.TransitionToScene("MainMenuScene");
    }
}
