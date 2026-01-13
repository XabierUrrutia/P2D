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

    private bool suppressUiEvents;
    private float prevMusic = 1f;
    private float prevSfx = 1f;
    private float prevVoice = 1f;

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
        if (suppressUiEvents) return;
        if (SoundColector.Instance == null) return;
        SoundColector.Instance.SetMusicVolume01(value);

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (suppressUiEvents) return;
        if (SoundColector.Instance == null) return;
        SoundColector.Instance.SetSfxVolume01(value);

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnVoiceVolumeChanged(float value)
    {
        if (suppressUiEvents) return;
        if (SoundColector.Instance == null) return;
        SoundColector.Instance.SetVoiceVolume01(value);

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

public void OnMuteAllChanged(bool muted)
{
    if (SoundColector.Instance == null) return;

    suppressUiEvents = true;

    if (muted)
    {
        // guardar valores atuais
        prevMusic = musicVolumeSlider.value;
        prevSfx   = sfxVolumeSlider.value;
        prevVoice = voiceVolumeSlider.value;

        // colocar sliders a 0 sem disparar callbacks
        musicVolumeSlider.value = 0f;
        sfxVolumeSlider.value   = 0f;
        voiceVolumeSlider.value = 0f;

        SoundColector.Instance.SetMuteAll(true);
    }
    else
    {
        // restaurar sliders sem disparar callbacks
        musicVolumeSlider.value = prevMusic;
        sfxVolumeSlider.value   = prevSfx;
        voiceVolumeSlider.value = prevVoice;

        SoundColector.Instance.SetMuteAll(false);

        // aplicar volumes restaurados
        SoundColector.Instance.SetMusicVolume01(prevMusic);
        SoundColector.Instance.SetSfxVolume01(prevSfx);
        SoundColector.Instance.SetVoiceVolume01(prevVoice);
    }

    suppressUiEvents = false;
}

}