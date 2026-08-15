using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BinResultSceneHandler: MonoBehaviour
{
    public void LoadOCRScene()
    {
        SceneManager.LoadScene("OCRScene");
    }
    
    public void LoadMainMenuScene()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
