using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuHandler : MonoBehaviour
{

    public void LoadTrashScannerScene()
    {
        SceneManager.LoadScene("AIScannerScene");
    }

    public void LoadCardCollectionScene()
    {
        SceneManager.LoadScene("CardCollectionScene");
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("UiCreation");
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
