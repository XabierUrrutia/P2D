using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class OptionsMenuUI : MonoBehaviour
{
    [Header("Áudio")]
    public Slider masterVolumeSlider;
    public Toggle muteToggle;

    [Header("Vídeo")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("[OptionsMenuUI] SettingsManager.Instance é null. Certifica-te que existe um SettingsManager numa cena anterior.");
            return;
        }

        InitAudioUI();
        InitResolutionUI();
    }

    void InitAudioUI()
    {
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        muteToggle.onValueChanged.RemoveAllListeners();

        masterVolumeSlider.value = SettingsManager.Instance.masterVolume;
        muteToggle.isOn = SettingsManager.Instance.isMuted;

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        muteToggle.onValueChanged.AddListener(OnMuteChanged);
    }

    void InitResolutionUI()
    {
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.RemoveAllListeners();

        var resolutions = SettingsManager.Instance.availableResolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution r = resolutions[i];
            options.Add($"{r.width} x {r.height} @ {r.refreshRate}Hz");
        }

        resolutionDropdown.AddOptions(options);

        resolutionDropdown.value = SettingsManager.Instance.currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        fullscreenToggle.isOn = SettingsManager.Instance.isFullscreen;

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    void OnMasterVolumeChanged(float value)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.SetMasterVolume(value);
    }

    void OnMuteChanged(bool muted)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.SetMuted(muted);
    }

    void OnResolutionChanged(int index)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.SetResolution(index, SettingsManager.Instance.isFullscreen);
    }

    void OnFullscreenChanged(bool fullscreen)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.SetFullscreen(fullscreen);
    }
}