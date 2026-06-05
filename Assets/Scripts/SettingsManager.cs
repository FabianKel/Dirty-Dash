using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    const string MasterVolumeKey = "settings.masterVolume";
    const string MusicVolumeKey = "settings.musicVolume";
    const string SfxVolumeKey = "settings.sfxVolume";
    const float DefaultVolume = 1f;
    const float MinSliderVolume = 0.0001f;

    public static float CurrentMasterVolume { get; private set; } = DefaultVolume;
    public static float CurrentMusicVolume { get; private set; } = DefaultVolume;
    public static float CurrentSfxVolume { get; private set; } = DefaultVolume;

    public static event System.Action VolumesChanged;

    public AudioMixer mainMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Awake()
    {
        ResolveSliders();
        LoadVolumes();
        BindSliders();
        RefreshSliderUI();
        ApplyAllVolumes();
    }

    void OnEnable()
    {
        ApplyAllVolumes();
        RefreshSliderUI();
    }

    public void SetMasterVolume(float volume)
    {
        CurrentMasterVolume = SanitizeSliderVolume(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, CurrentMasterVolume);
        ApplyMixerVolume("MasterVol", CurrentMasterVolume);
        AudioListener.volume = CurrentMasterVolume;
        NotifyVolumesChanged();
    }

    public void SetMusicVolume(float volume)
    {
        CurrentMusicVolume = SanitizeSliderVolume(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, CurrentMusicVolume);
        ApplyMixerVolume("MusicVol", CurrentMusicVolume);
        NotifyVolumesChanged();
    }

    public void SetSFXVolume(float volume)
    {
        CurrentSfxVolume = SanitizeSliderVolume(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, CurrentSfxVolume);
        ApplyMixerVolume("SFXVol", CurrentSfxVolume);
        NotifyVolumesChanged();
    }

    void ResolveSliders()
    {
        if (masterSlider == null) masterSlider = FindSlider("MasterSlider");
        if (musicSlider == null) musicSlider = FindSlider("MusicSlider");
        if (sfxSlider == null) sfxSlider = FindSlider("SFXSlider");
    }

    Slider FindSlider(string sliderName)
    {
        Slider[] sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null || slider.name != sliderName) continue;
            if (slider.gameObject.scene != gameObject.scene) continue;
            return slider;
        }

        return null;
    }

    void BindSliders()
    {
        BindSlider(masterSlider, SetMasterVolume);
        BindSlider(musicSlider, SetMusicVolume);
        BindSlider(sfxSlider, SetSFXVolume);
    }

    void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.RemoveListener(callback);
        slider.onValueChanged.AddListener(callback);
    }

    void LoadVolumes()
    {
        CurrentMasterVolume = SanitizeSliderVolume(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        CurrentMusicVolume = SanitizeSliderVolume(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));
        CurrentSfxVolume = SanitizeSliderVolume(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume));
    }

    void RefreshSliderUI()
    {
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(CurrentMasterVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(CurrentMusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(CurrentSfxVolume);
    }

    void ApplyAllVolumes()
    {
        ApplyMixerVolume("MasterVol", CurrentMasterVolume);
        ApplyMixerVolume("MusicVol", CurrentMusicVolume);
        ApplyMixerVolume("SFXVol", CurrentSfxVolume);
        AudioListener.volume = CurrentMasterVolume;
        NotifyVolumesChanged();
    }

    void ApplyMixerVolume(string exposedParameter, float volume)
    {
        if (mainMixer == null) return;

        float db = volume <= MinSliderVolume ? -80f : Mathf.Log10(volume) * 20f;
        mainMixer.SetFloat(exposedParameter, db);
    }

    float SanitizeSliderVolume(float volume)
    {
        return Mathf.Clamp01(volume);
    }

    void NotifyVolumesChanged()
    {
        PlayerPrefs.Save();
        VolumesChanged?.Invoke();
    }
}
