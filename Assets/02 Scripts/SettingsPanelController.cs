using UnityEngine;
using UnityEngine.UI;
using static Constants;

public class SettingsPanelController : PanelController
{
    private AudioManager audioManager;
    [SerializeField] private Slider effectVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    public void OnEnable()
    {
        audioManager = FindObjectOfType<AudioManager>();
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        // 저장된 값이 있으면 불러오고, 없으면 프리팹의 슬라이더 기본값 사용
        float savedEffectVolume = PlayerPrefs.GetFloat(EFFECT_VOLUME_KEY, effectVolumeSlider.value);
        float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, musicVolumeSlider.value);
        
        effectVolumeSlider.value = savedEffectVolume;
        musicVolumeSlider.value = savedMusicVolume;
        
        audioManager.SetSFXVolume(savedEffectVolume);
        audioManager.SetMusicVolume(savedMusicVolume);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(EFFECT_VOLUME_KEY, effectVolumeSlider.value);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolumeSlider.value);
        
        PlayerPrefs.Save();
    }

    public void OnSlideEffectVolume()
    {
        float volume = effectVolumeSlider.value;
        audioManager.SetSFXVolume(volume);
    }

    public void OnSlideMusicVolume()
    {
        float volume = musicVolumeSlider.value;
        audioManager.SetMusicVolume(volume);
    }

    // x 버튼 누르면
    public void OnClickCloseButton()
    {
        SaveSettings();
        
        Hide();
    }
}