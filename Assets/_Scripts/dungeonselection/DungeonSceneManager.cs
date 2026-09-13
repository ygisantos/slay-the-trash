using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonSceneManager : MonoBehaviour
{
    [Header("Dungeon Scenes (Editable in Inspector)")]
    public string dungeonScene1;
    public string dungeonScene2;
    public string dungeonScene3;

    [Header("Return Scene")]
    public string returnScene;

    // Call this to load a specific dungeon
    public void LoadDungeon1()
    {
        if (!string.IsNullOrEmpty(dungeonScene1))
        {
            Debug.Log($"Loading scene: {dungeonScene1}");
            Transitioner.Instance.TransitionToScene(dungeonScene1);
        }
    }

    public void LoadDungeon2()
    {
        if (!string.IsNullOrEmpty(dungeonScene2))
        {
            Debug.Log($"Loading scene: {dungeonScene2}");
            Transitioner.Instance.TransitionToScene(dungeonScene2);
        }
    }

    public void LoadDungeon3()
    {
        if (!string.IsNullOrEmpty(dungeonScene3))
        {
            Debug.Log($"Loading scene: {dungeonScene3}");
            Transitioner.Instance.TransitionToScene(dungeonScene3);
        }
    }

    public void ReturnToScene()
    {
        if (!string.IsNullOrEmpty(returnScene))
        {
            Debug.Log($"Returning to scene: {returnScene}");
            Transitioner.Instance.TransitionToScene(returnScene);
        }
    }
}
