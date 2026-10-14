using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ThrowingInstructionsSceneHandler: MonoBehaviour
{
    private bool isLoading = false;
    public void LoadTrashScannerScene()
    {
        Transitioner.Instance.TransitionToScene("AIScannerScene");
    }
    
    public void LoadOCRSceneScene()
    {
        Transitioner.Instance.TransitionToScene("OCRScene");
    }

    
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadWithDelay(sceneName));
    }

    public void LoadDungeon()
    {
        StartCoroutine(LoadWithDelay("DungeonSelection"));
    }
    private IEnumerator LoadWithDelay(string sceneName)
    {
        if (isLoading) yield break;
        isLoading = true;

        yield return new WaitForSeconds(1.5f);
        Transitioner.Instance.TransitionToScene(sceneName);
    }
}
