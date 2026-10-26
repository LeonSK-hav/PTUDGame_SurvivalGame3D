using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myAudioMixer;

    // Gọi hàm này khi kéo thanh trượt Sound
    Animator animator;
    public void SetSoundVolume(float sliderValue)
    {
        // Chuyển giá trị Slider (0.0001 đến 1) thành Decibel (-80dB đến 20dB)
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        myAudioMixer.SetFloat("MasterVolume", volume);
    }

    // Gọi hàm này khi kéo thanh trượt Effect
    public void SetEffectVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        myAudioMixer.SetFloat("EffectVolume", volume);
    }
}
