using UnityEngine;

public class PlaySoundOnSpawn : MonoBehaviour
{
    [Header("Sound Settings")]
    public SoundType sound;                  // The sound to play
    [Range(0f, 1f)] public float volume = 1f; // Optional volume multiplier
    [Range(0.1f, 3f)] public float pitch = 1f; // Optional pitch override
    public bool useCustomPitch = false;      // Should we override the default pitch?

    private void Awake()
    {
        PlaySound();
    }

    public void PlaySound()
    {
        if (useCustomPitch)
        {
            SoundManager.PlaySoundWithCustomPitch(sound, pitch, volume);
        }
        else
        {
            SoundManager.PlaySound(sound, volume);
        }
    }
}
