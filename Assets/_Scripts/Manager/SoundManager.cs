using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum SoundCategory
{
 CARD,
 PLAYER,
 ENEMY,
 STATUSEFFECTS,
 EFFECTS,
 MUSIC

}

public enum SoundType
{
    //CARD
    CARDPRESS,
    CAMERA,

    // PLAYER
    PLAYERHURT,
   // ENEMY
   SMALLENEMYHURT,
   MEDIUMENEMYHURT,
   BIGENEMYHURT,
   BOSSENEMYHURT,
   // STATUSEFFECTS
    ARMORUP,
    ARMORHIT,
    BLOODSPLAT,
    //EFFECTS 
    SHURIKEN,
    SLAM,
    SLASH,
    CANON,
    BLASTER,
    HEALTH,
    //  MUSIC
    GAMEMUSIC,
    BOSSMUSIC,
    MENUMUSIC,
    WIN,
    LOSE,
    CLICK,


}

[Serializable]
public struct CategorySoundList
{
    [HideInInspector] public string categoryName;
    public SoundCategory category;
    public SoundList[] sounds;
}

[Serializable]
public struct SoundList
{
    public SoundType soundType;

    [Tooltip("Add multiple clips for variation")]
    public AudioClip[] clips;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float volume;

    [Header("Pitch Settings")]
    [Range(0.1f, 3f)] public float pitch;
    public bool randomizePitch;
    [Range(0.1f, 3f)] public float minPitch;
    [Range(0.1f, 3f)] public float maxPitch;

    [Header("Spatial Settings")]
    public bool is3D;
}

[RequireComponent(typeof(AudioSource))]
[ExecuteInEditMode]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private CategorySoundList[] categories;
    [SerializeField] private AudioSource musicSource;

    private static SoundManager instance;

    private Dictionary<SoundType, AudioSource> loopingSounds = new Dictionary<SoundType, AudioSource>();

    // Master volume multipliers
    private static float sfxVolumeMultiplier = 1f;
    private static float musicVolumeMultiplier = 1f;

    // Track active coroutines
    private Coroutine fadeOutCoroutine;
    private Coroutine fadeInCoroutine;

    private void Awake()
    {
   
        if (!Application.isPlaying) return;

        float savedMusic = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : 1f;
        float savedSFX = PlayerPrefs.HasKey("SFXVolume") ? PlayerPrefs.GetFloat("SFXVolume") : 1f;

        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);


        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private SoundList GetSoundData(SoundType sound)
    {
        if (categories == null) return default;
        foreach (var category in categories)
        {
            if (category.sounds == null) continue;
            foreach (var soundData in category.sounds)
            {
                if (soundData.soundType == sound)
                {
                    return soundData;
                }
            }
        }
        return default;
    }

    // Play one-shot sound effect
    public static void PlaySound(SoundType sound, float volumeMultiplier = 1)
    {
        if (instance == null)
        {
            Debug.LogWarning("SoundManager instance is null!");
            return;
        }

        SoundList soundData = instance.GetSoundData(sound);
        if (soundData.clips == null || soundData.clips.Length == 0)
        {
            Debug.LogWarning($"No audio clips assigned for {sound}");
            return;
        }

        AudioClip randomClip = soundData.clips[UnityEngine.Random.Range(0, soundData.clips.Length)];

        float finalVolume = soundData.volume * volumeMultiplier * sfxVolumeMultiplier;
        float finalPitch = soundData.randomizePitch
            ? UnityEngine.Random.Range(soundData.minPitch, soundData.maxPitch)
            : soundData.pitch;

        GameObject tempObj = new GameObject("TempSound_" + sound.ToString());
        tempObj.transform.SetParent(instance.transform);
        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.clip = randomClip;
        tempSource.volume = finalVolume;
        tempSource.pitch = finalPitch;
        tempSource.spatialBlend = soundData.is3D ? 1f : 0f;
        tempSource.Play();

        Destroy(tempObj, randomClip.length / finalPitch + 0.5f);
    }

    // Play looping sound
    public static void PlayLoopingSound(SoundType sound, float volumeMultiplier = 1)
    {
        if (instance == null) return;

        StopLoopingSound(sound);

        SoundList soundData = instance.GetSoundData(sound);
        if (soundData.clips == null || soundData.clips.Length == 0) return;

        AudioClip randomClip = soundData.clips[UnityEngine.Random.Range(0, soundData.clips.Length)];

        GameObject soundObj = new GameObject("Loop_" + sound.ToString());
        soundObj.transform.SetParent(instance.transform);
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = randomClip;
        source.volume = soundData.volume * volumeMultiplier * sfxVolumeMultiplier;
        source.pitch = soundData.randomizePitch
            ? UnityEngine.Random.Range(soundData.minPitch, soundData.maxPitch)
            : soundData.pitch;
        source.spatialBlend = soundData.is3D ? 1f : 0f;
        source.loop = true;
        source.Play();

        instance.loopingSounds[sound] = source;
    }

    public static void StopLoopingSound(SoundType sound)
    {
        if (instance == null) return;

        if (instance.loopingSounds.ContainsKey(sound))
        {
            AudioSource source = instance.loopingSounds[sound];
            if (source != null)
            {
                Destroy(source.gameObject);
            }
            instance.loopingSounds.Remove(sound);
        }
    }

    public static void StopAllLoopingSounds()
    {
        if (instance == null) return;

        foreach (var kvp in instance.loopingSounds)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        instance.loopingSounds.Clear();
    }

    // Play music
    public static void PlayMusic(SoundType music, float volumeMultiplier = 1f, bool loop = true)
    {
        if (instance == null || instance.musicSource == null) return;

        // Stop any active fade coroutines
        if (instance.fadeOutCoroutine != null)
        {
            instance.StopCoroutine(instance.fadeOutCoroutine);
            instance.fadeOutCoroutine = null;
        }
        if (instance.fadeInCoroutine != null)
        {
            instance.StopCoroutine(instance.fadeInCoroutine);
            instance.fadeInCoroutine = null;
        }

        SoundList soundData = instance.GetSoundData(music);
        if (soundData.clips == null || soundData.clips.Length == 0) return;

        AudioClip clip = soundData.clips[0];

        instance.musicSource.clip = clip;
        instance.musicSource.volume = soundData.volume * volumeMultiplier * musicVolumeMultiplier;
        instance.musicSource.pitch = soundData.pitch;
        instance.musicSource.loop = loop;
        instance.musicSource.Play();
    }

    public static void StopMusic()
    {
        if (instance != null && instance.musicSource != null)
        {
            // Stop any active fade coroutines
            if (instance.fadeOutCoroutine != null)
            {
                instance.StopCoroutine(instance.fadeOutCoroutine);
                instance.fadeOutCoroutine = null;
            }
            if (instance.fadeInCoroutine != null)
            {
                instance.StopCoroutine(instance.fadeInCoroutine);
                instance.fadeInCoroutine = null;
            }

            instance.musicSource.Stop();
        }
    }

    public static void FadeOutMusic(float duration = 1f)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.FadeOutMusicCoroutine(duration));
        }
    }

    private IEnumerator FadeOutMusicCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
    }

    public static void FadeInMusic(SoundType music, float targetVolumeMultiplier = 1f, float duration = 1f)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.FadeInMusicCoroutine(music, targetVolumeMultiplier, duration));
        }
    }

    private IEnumerator FadeInMusicCoroutine(SoundType music, float targetVolumeMultiplier, float duration)
    {
        SoundList soundData = GetSoundData(music);
        if (soundData.clips == null || soundData.clips.Length == 0) yield break;

        AudioClip clip = soundData.clips[0];
        float targetVolume = soundData.volume * targetVolumeMultiplier * musicVolumeMultiplier;

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.pitch = soundData.pitch;
        musicSource.loop = true;
        musicSource.Play();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    // Set master SFX volume (affects all sound effects)
    public static void SetSFXVolume(float volume)
    {
        sfxVolumeMultiplier = Mathf.Clamp01(volume);

        // Update all currently playing looping sounds
        if (instance != null)
        {
            foreach (var kvp in instance.loopingSounds)
            {
                if (kvp.Value != null)
                {
                    SoundList soundData = instance.GetSoundData(kvp.Key);
                    kvp.Value.volume = soundData.volume * sfxVolumeMultiplier;
                }
            }
        }
    }

    // Set master music volume
    public static void SetMusicVolume(float volume)
    {
        musicVolumeMultiplier = Mathf.Clamp01(volume);

        if (instance != null && instance.musicSource != null && instance.musicSource.clip != null)
        {
            // Get the current music's sound data to maintain its base volume
            SoundType? currentMusicType = null;
            foreach (var category in instance.categories)
            {
                foreach (var soundData in category.sounds)
                {
                    if (soundData.clips != null && soundData.clips.Length > 0 &&
                        soundData.clips[0] == instance.musicSource.clip)
                    {
                        currentMusicType = soundData.soundType;
                        break;
                    }
                }
                if (currentMusicType.HasValue) break;
            }

            if (currentMusicType.HasValue)
            {
                SoundList soundData = instance.GetSoundData(currentMusicType.Value);
                instance.musicSource.volume = soundData.volume * musicVolumeMultiplier;
            }
        }
    }

    // Get current SFX volume
    public static float GetSFXVolume()
    {
        return sfxVolumeMultiplier;
    }

    // Get current music volume
    public static float GetMusicVolume()
    {
        return musicVolumeMultiplier;
    }

    public static void PauseMusic()
    {
        if (instance != null && instance.musicSource != null)
        {
            instance.musicSource.Pause();
        }
    }

    public static void ResumeMusic()
    {
        if (instance != null && instance.musicSource != null)
        {
            instance.musicSource.UnPause();
        }
    }

    public static bool IsMusicPlaying()
    {
        return instance != null && instance.musicSource != null && instance.musicSource.isPlaying;
    }
    public static void TransitionMusic(SoundType newMusic, float fadeOutDuration = 1f, float fadeInDuration = 1f, float volumeMultiplier = 1f)
    {
        if (instance == null || instance.musicSource == null) return;

        // Stop any current transition
        if (instance.fadeOutCoroutine != null) instance.StopCoroutine(instance.fadeOutCoroutine);
        if (instance.fadeInCoroutine != null) instance.StopCoroutine(instance.fadeInCoroutine);

        instance.StartCoroutine(instance.TransitionMusicCoroutine(newMusic, fadeOutDuration, fadeInDuration, volumeMultiplier));
    }

    private IEnumerator TransitionMusicCoroutine(SoundType newMusic, float fadeOutDuration, float fadeInDuration, float volumeMultiplier)
    {
        // Fade out current music
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            musicSource.Stop();
            musicSource.volume = startVolume; // reset for fade-in
        }

        // Start new music with fade-in
        SoundList soundData = GetSoundData(newMusic);
        if (soundData.clips == null || soundData.clips.Length == 0) yield break;

        musicSource.clip = soundData.clips[0];
        musicSource.volume = 0f;
        musicSource.pitch = soundData.pitch;
        musicSource.loop = true;
        musicSource.Play();

        float targetVolume = soundData.volume * volumeMultiplier * musicVolumeMultiplier;
        float fadeElapsed = 0f;

        while (fadeElapsed < fadeInDuration)
        {
            fadeElapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, fadeElapsed / fadeInDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }
    public static void PlaySoundAtPosition(SoundType sound, Vector3 position, float volumeMultiplier = 1f)
    {
        if (instance == null) return;

        SoundList soundData = instance.GetSoundData(sound);
        if (soundData.clips == null || soundData.clips.Length == 0) return;

        AudioClip randomClip = soundData.clips[UnityEngine.Random.Range(0, soundData.clips.Length)];
        float finalVolume = soundData.volume * volumeMultiplier * sfxVolumeMultiplier;

        AudioSource.PlayClipAtPoint(randomClip, position, finalVolume);
    }

    public static void PlaySoundWithCustomPitch(SoundType sound, float customPitch, float volumeMultiplier = 1f)
    {
        if (instance == null) return;

        SoundList soundData = instance.GetSoundData(sound);
        if (soundData.clips == null || soundData.clips.Length == 0) return;

        AudioClip randomClip = soundData.clips[UnityEngine.Random.Range(0, soundData.clips.Length)];

        GameObject soundObj = new GameObject("CustomPitchSound");
        soundObj.transform.SetParent(instance.transform);
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = randomClip;
        source.volume = soundData.volume * volumeMultiplier * sfxVolumeMultiplier;
        source.pitch = customPitch;
        source.spatialBlend = soundData.is3D ? 1f : 0f;
        source.Play();

        Destroy(soundObj, randomClip.length / customPitch + 0.5f);
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        // Auto-create categories if needed
        if (categories == null || categories.Length == 0)
        {
            string[] categoryNames = Enum.GetNames(typeof(SoundCategory));
            categories = new CategorySoundList[categoryNames.Length];
            for (int i = 0; i < categoryNames.Length; i++)
            {
                categories[i].categoryName = categoryNames[i];
                categories[i].category = (SoundCategory)i;
            }
        }
    }
#endif
}
