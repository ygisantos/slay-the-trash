using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneHandler : MonoBehaviour
{
    public void Start()
    {
        SoundManager.Instance.PlayMusic(SoundType.MENUMUSIC);
    }
    public void ToGameMedium()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        Transitioner.Instance.TransitionToScene("MainGame2");
    }
    public void ToGameBoss()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        Transitioner.Instance.TransitionToScene("MainGame3");
    }

    public void ToGameScene (string scene)
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        Transitioner.Instance.TransitionToScene(scene);
    }
}
