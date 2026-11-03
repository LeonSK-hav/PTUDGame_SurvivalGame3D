using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer myAudioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectSource;

    private const string MasterVolumeKey = "MasterVolume";
    private const string EffectVolumeKey = "EffectVolume";

    private void Awake()
    {
        // Singleton pattern để giữ AudioManager không bị mất khi chuyển Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadVolumeSettings();
        PlayMusic(musicSource.clip, true); // Phát nhạc nền khi bắt đầu
    }

    // Phát âm thanh hiệu ứng 2D (như nhặt đồ, click button, tiếng súng)
    public void PlayEffectSound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && effectSource != null)
        {
           // effectSource.PlayOneShot(clip, volumeScale);
           effectSource.Stop(); // Dừng âm thanh cũ ngay lập tức
        effectSource.clip = clip;
        effectSource.volume = volumeScale;
        effectSource.Play(); // Phát âm thanh mới từ đầu
        }
    }
    public void StopEffectSound()
    {
        if (effectSource != null)
        {
           effectSource.Stop(); // Dừng âm thanh cũ ngay lập tức
       
        }
    }

    // Phát nhạc nền
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }

    // Tải và áp dụng âm lượng đã lưu từ PlayerPrefs khi chuyển Scene
    private void LoadVolumeSettings()
    {
        float savedSoundVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float savedEffectVolume = PlayerPrefs.GetFloat(EffectVolumeKey, 1f);

        SetMasterVolumeDirect(savedSoundVolume);
        SetEffectVolumeDirect(savedEffectVolume);
    }

    public void SetMasterVolume(float sliderValue)
    {
        SetMasterVolumeDirect(sliderValue);
        PlayerPrefs.SetFloat(MasterVolumeKey, sliderValue);
        PlayerPrefs.Save();
    }

    public void SetEffectVolume(float sliderValue)
    {
        SetEffectVolumeDirect(sliderValue);
        PlayerPrefs.SetFloat(EffectVolumeKey, sliderValue);
        PlayerPrefs.Save();
    }

    private void SetMasterVolumeDirect(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        if (myAudioMixer != null) myAudioMixer.SetFloat("MasterVolume", volume);
    }

    private void SetEffectVolumeDirect(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        if (myAudioMixer != null) myAudioMixer.SetFloat("EffectVolume", volume);
    }
}