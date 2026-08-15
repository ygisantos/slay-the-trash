using UnityEngine;

public class PlayMusicOnSpawn : MonoBehaviour
{
    [Header("Music To Play On Spawn")]
    public SoundType musicToPlay = SoundType.GAMEMUSIC;

    [Header("Settings")]
    public float volumeMultiplier = 1f;
    public bool loop = true;
    public bool fadeIn = false;
    public float fadeInDuration = 1f;

    void Start()
    {
        if (fadeIn)
        {
            SoundManager.FadeInMusic(musicToPlay, volumeMultiplier, fadeInDuration);
        }
        else
        {
            SoundManager.PlayMusic(musicToPlay, volumeMultiplier, loop);
        }
    }
}
