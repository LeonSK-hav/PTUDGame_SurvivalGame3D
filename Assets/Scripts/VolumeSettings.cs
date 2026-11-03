using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myAudioMixer;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider effectSlider;

    private const string MasterVolumeKey = "MasterVolume";
    private const string EffectVolumeKey = "EffectVolume";

    private void Start()
    {
        /*
        // Load saved volume values or default to 1.0 if nothing is saved yet
        float savedSoundVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float savedEffectVolume = PlayerPrefs.GetFloat(EffectVolumeKey, 1f);

        // Set the slider UI positions if assigned
        if (soundSlider != null) soundSlider.value = savedSoundVolume;
        if (effectSlider != null) effectSlider.value = savedEffectVolume;

        // Apply volumes to the AudioMixer
        ApplyMasterVolume(savedSoundVolume);
        ApplyEffectVolume(savedEffectVolume);*/
    }
    private void OnEnable()
{
    float savedSoundVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
    float savedEffectVolume = PlayerPrefs.GetFloat(EffectVolumeKey, 1f);

    if (soundSlider != null) soundSlider.value = savedSoundVolume;
    if (effectSlider != null) effectSlider.value = savedEffectVolume;

    ApplyMasterVolume(savedSoundVolume);
    ApplyEffectVolume(savedEffectVolume);
}

    // Call this method when dragging the Master/Sound slider
    public void SetSoundVolume(float sliderValue)
    {
        ApplyMasterVolume(sliderValue);
        PlayerPrefs.SetFloat(MasterVolumeKey, sliderValue);
        PlayerPrefs.Save();
    }

    // Call this method when dragging the Effect slider
    public void SetEffectVolume(float sliderValue)
    {
        ApplyEffectVolume(sliderValue);
        PlayerPrefs.SetFloat(EffectVolumeKey, sliderValue);
        PlayerPrefs.Save();
    }

    private void ApplyMasterVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        myAudioMixer.SetFloat("MasterVolume", volume);
    }

    private void ApplyEffectVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        myAudioMixer.SetFloat("EffectVolume", volume);
    }
}