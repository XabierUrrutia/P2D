using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class OptionsMenuUI : MonoBehaviour
{
    [Header("Áudio")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider voiceVolumeSlider;

    [Tooltip("Quando ligado, o jogo fica totalmente sem som (música, SFX e voz).")]
    public Toggle muteAllToggle;

    private void Start()
    {
        if (SoundColector.Instance == null)
        {
            Debug.LogWarning("[OptionsMenuUI] SoundColector.Instance é null. Verifica se o objeto SoundColector existe na primeira cena.");
            return;
        }

        InitAudioUI();
    }

    private void InitAudioUI()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.value = SoundColector.Instance.musicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.value = SoundColector.Instance.sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.onValueChanged.RemoveAllListeners();
            voiceVolumeSlider.value = SoundColector.Instance.voiceVolume;
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        }

        if (muteAllToggle != null)
        {
            muteAllToggle.onValueChanged.RemoveAllListeners();

            // Estado inicial do toggle baseado no SoundColector
            bool isMuted = SoundColector.Instance.muteAll;
            muteAllToggle.isOn = isMuted;
            muteAllToggle.onValueChanged.AddListener(OnMuteAllChanged);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (SoundColector.Instance == null) return;
        SoundColector.Instance.SetMusicVolume01(value);

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (SoundColector.Instance == null) return;
        SoundColector.Instance.SetSfxVolume01(value);

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnVoiceVolumeChanged(float value)
    {
        if (SoundColector.Instance == null) return;
        SoundColector.Instance.SetVoiceVolume01(value);

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnMuteAllChanged(bool muted)
    {
        if (SoundColector.Instance == null) return;

        SoundColector.Instance.SetMuteAll(muted);

        // Opcional: sincronizar sliders com 0 quando mutado
        if (muted)
        {
            if (musicVolumeSlider != null) musicVolumeSlider.value = 0f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0f;
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = 0f;
        }
        else
        {
            // Sliders refletem os valores atuais do SoundColector (carregados de prefs)
            if (musicVolumeSlider != null) musicVolumeSlider.value = SoundColector.Instance.musicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = SoundColector.Instance.sfxVolume;
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = SoundColector.Instance.voiceVolume;
        }
    }
}