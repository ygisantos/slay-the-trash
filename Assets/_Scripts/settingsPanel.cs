using UnityEngine;
using UnityEngine.UI;

public class settingsPanel : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";

    void Start()
    {
        // Load saved values
        float musicVolume = PlayerPrefs.HasKey(MusicVolumeKey) ? PlayerPrefs.GetFloat(MusicVolumeKey) : 1f;
        float sfxVolume = PlayerPrefs.HasKey(SFXVolumeKey) ? PlayerPrefs.GetFloat(SFXVolumeKey) : 1f;

        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        // Apply initial values
        SoundManager.SetMusicVolume(musicVolume);
        SoundManager.SetSFXVolume(sfxVolume);
    }

    private void OnMusicSliderChanged(float value)
    {
        SoundManager.SetMusicVolume(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
    }

    private void OnSFXSliderChanged(float value)
    {
        SoundManager.SetSFXVolume(value);
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
    }
}
