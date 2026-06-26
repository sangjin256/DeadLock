using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public static bool fullscreenIsOn = true;
    public static int resolutionIndex = 1;
    public static float BGMValue = 1f;
    public static float EffectValue = 1f;
    public static bool BGMisOn = true;
    public static bool EffectisOn = true;

    public GameObject creditPanel;

    public Toggle fullscreen;
    public Dropdown resolution;
    public Slider BGM;
    public Slider Effect;

    public void Start()
    {
        switch (Screen.width)
        {
            case 1280:
                resolution.value = 0;
                break;
            case 1920:
                resolution.value = 1;
                break;
            case 2560:
                resolution.value = 2;
                break;
            case 3840:
                resolution.value = 3;
                break;
            default:
                break;
        }
        fullscreen.isOn = fullscreenIsOn;
        resolution.value = resolutionIndex;
        BGM.value = BGMValue;
        Effect.value = EffectValue;
        if (BGMisOn == false) BGM.value = 0;
        if (EffectisOn == false) Effect.value = 0;
        BGMisOn = EffectisOn = true;
    }

    public void ResolutionDropdownChanged(Dropdown change)
    {
        resolutionIndex = change.value;
        switch (resolutionIndex)
        {
            case 0:
                Screen.SetResolution(1280, 720, Screen.fullScreen);
                break;
            case 1:
                Screen.SetResolution(1920, 1080, Screen.fullScreen);
                break;
            case 2:
                Screen.SetResolution(2560, 1440, Screen.fullScreen);
                break;
            case 3:
                Screen.SetResolution(3840, 2160, Screen.fullScreen);
                break;
        }
    }

    public void FullscreenToggleChanged(Toggle toggle)
    {
        Screen.SetResolution(Screen.width, Screen.height, toggle.isOn);
        fullscreenIsOn = toggle.isOn;
    }

    public void BGMVolumeChanged(Slider slider)
    {
        if (slider.value > 0.000001 && GameManager.instance.backgroundMusic.isPlaying == false) GameManager.instance.backgroundMusic.Play();
        GameManager.instance.backgroundMusic.volume = slider.value;
        BGMValue = slider.value;
    }

    public void EffectVolumeChanged(Slider slider)
    {
        List<AudioSource> effectSounds = GameManager.instance.effectSounds;

        if (effectSounds[1].isPlaying == false) effectSounds[1].Play();

        for(int i = 0; i < effectSounds.Count; i++)
        {
            effectSounds[i].volume = slider.value;
        }
        EffectValue = slider.value;
    }
}
