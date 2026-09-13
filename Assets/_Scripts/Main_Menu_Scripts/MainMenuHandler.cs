using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuHandler : MonoBehaviour
{

    public void LoadTrashScannerScene()
    {
        Transitioner.Instance.TransitionToScene("AIScannerScene");
    }

    public void LoadCardCollectionScene()
    {
        Transitioner.Instance.TransitionToScene("CardCollectionScene");
    }

    public void LoadGame()
    {
        Transitioner.Instance.TransitionToScene("UiCreation");
    }

    public void QuitGame()
    {
        Debug.Log("Game Exited");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
