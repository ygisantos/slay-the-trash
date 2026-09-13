using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneHandler : MonoBehaviour
{
    public void Start()
    {
        SoundManager.PlayMusic(SoundType.MENUMUSIC);
    }
    public void ToGameMedium()
    {
        SoundManager.PlaySound(SoundType.CLICK);
        Transitioner.Instance.TransitionToScene("MainGame2");
    }
    public void ToGameBoss()
    {
        SoundManager.PlaySound(SoundType.CLICK);
        Transitioner.Instance.TransitionToScene("MainGame3");
    }

    public void ToGameScene (string scene)
    {
        SoundManager.PlaySound(SoundType.CLICK);
        Transitioner.Instance.TransitionToScene(scene);
    }
}
