using UnityEngine;
using UnityEngine.SceneManagement;
using static Constants;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip buttonHoverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip stonePlaceSfx;
    [SerializeField] private AudioClip winSfx;
    [SerializeField] private AudioClip loseSfx;

    [Header("Settings")]
    [SerializeField] private float musicVolume = .7f;
    [SerializeField] private float sfxVolume = .5f;

    protected override void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        // 씬에 따라 배경음악 변경
        if (scene.name == Constants.SCENE_MAIN)
        {
            PlayMusic(mainMenuMusic);
        }
        else if (scene.name == Constants.SCENE_GAME)
        {
            PlayMusic(gameMusic);
        }
    }

    private void Start()
    {
        // PlayerPrefs에서 저장된 볼륨 불러오기 (없으면 Inspector의 기본값 사용)
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(EFFECT_VOLUME_KEY, sfxVolume);

        // 초기 볼륨 설정
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.loop = true;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // 배경음악 재생
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    // 배경음악 정지
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // 효과음 재생
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayHoverButtonSfx()
    {
        PlaySFX(buttonHoverSfx);
    }

    // 버튼 클릭 효과음
    public void PlayButtonClickSfx()
    {
        PlaySFX(buttonClickSfx);
    }

    // 돌 놓는 효과음
    public void PlayStonePlaceSfx()
    {
        PlaySFX(stonePlaceSfx);
    }

    // 승리 효과음
    public void PlayWinSfx()
    {
        PlaySFX(winSfx);
    }

    // 패배 효과음
    public void PlayLoseSfx()
    {
        PlaySFX(loseSfx);
    }

    // 배경음악 볼륨 설정
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    // 효과음 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // 현재 볼륨 값 가져오기
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    // 음소거 상태 가져오기
    public bool IsMusicMuted() => musicSource != null && musicSource.mute;
    public bool IsSFXMuted() => sfxSource != null && sfxSource.mute;
}
