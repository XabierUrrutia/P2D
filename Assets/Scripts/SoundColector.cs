using System.Collections.Generic;
using UnityEngine;

public class SoundColector : MonoBehaviour
{
    // SINGLETON
    public static SoundColector Instance { get; private set; }

    [Header("Música de Fundo (opcional)")]
    [SerializeField] private AudioClip[] backgroundMusicClips;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;

    [Header("Configuração SFX")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;
    [SerializeField] private bool randomizeSfxPitch = true;
    [Range(0.5f, 1.5f)] [SerializeField] private float minSfxPitch = 0.9f;
    [Range(0.5f, 1.5f)] [SerializeField] private float maxSfxPitch = 1.1f;
    [SerializeField] private bool debugLogs = true;

    [Header("Clips – Edifícios")]
    public AudioClip Building_Exp_1;
    public AudioClip Building_Exp_2;

    [Header("Clips – Infantaria")]
    public AudioClip Infantry_1;
    public AudioClip Infantry_2;
    public AudioClip Infantry_3;
    public AudioClip Infantry_Moving;
    public AudioClip Infantry_Shot;
    public AudioClip InfantryDeath_1;
    public AudioClip InfantryDeath_2;
    public AudioClip InfantryDeath_3;

    [Header("Clips – Tanque")]
    public AudioClip Tank_1;
    public AudioClip Tank_2;
    public AudioClip Tank_3;
    public AudioClip Tank_Fire_1;
    public AudioClip Tank_Fire_2;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton básico
        if (Instance != null && Instance != this)
        {
            if (debugLogs)
                Debug.Log("[SoundColector] Já existia uma instância, destruindo esta nova.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugLogs)
            Debug.Log("[SoundColector] Awake -> Instance criada.");

        SetupAudioSources();
    }

    private void Start()
    {
        // Se tiver pelo menos uma música, toca logo em loop (ex: ost1 em backgroundMusicClips[0])
        if (backgroundMusicClips != null && backgroundMusicClips.Length > 0)
        {
            PlayRandomMusic(true);
        }
    }

    #endregion

    #region Setup

    private void SetupAudioSources()
    {
        // Música
        GameObject musicGO = new GameObject("MusicSource");
        musicGO.transform.SetParent(transform);
        musicSource = musicGO.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        // SFX
        GameObject sfxGO = new GameObject("SFXSource");
        sfxGO.transform.SetParent(transform);
        sfxSource = sfxGO.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    #endregion

    #region Helpers Internos

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            if (debugLogs)
                Debug.LogWarning("[SoundColector] Tentou tocar um clip nulo.");
            return;
        }

        if (randomizeSfxPitch)
            sfxSource.pitch = Random.Range(minSfxPitch, maxSfxPitch);
        else
            sfxSource.pitch = 1f;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private AudioClip GetRandomClip(params AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;

        List<AudioClip> valid = new List<AudioClip>();
        foreach (var c in clips)
            if (c != null) valid.Add(c);

        if (valid.Count == 0) return null;

        int index = Random.Range(0, valid.Count);
        return valid[index];
    }

    #endregion

    #region Música de Fundo

    /// <summary>
    /// Toca uma faixa específica pelo índice (caso queiras controlar manualmente).
    /// </summary>
    public void PlayMusic(int index, bool loop = true)
    {
        if (backgroundMusicClips == null || backgroundMusicClips.Length == 0)
        {
            if (debugLogs)
                Debug.LogWarning("[SoundColector] Nenhuma música atribuída em backgroundMusicClips.");
            return;
        }

        if (index < 0 || index >= backgroundMusicClips.Length)
        {
            if (debugLogs)
                Debug.LogWarning($"[SoundColector] Índice de música inválido: {index}");
            return;
        }

        musicSource.clip = backgroundMusicClips[index];
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();

        if (debugLogs)
            Debug.Log($"[SoundColector] PlayMusic index {index} ({musicSource.clip.name})");
    }

    /// <summary>
    /// Toca uma música aleatória da lista (com uma só, será sempre a ost1).
    /// </summary>
    public void PlayRandomMusic(bool loop = true)
    {
        if (backgroundMusicClips == null || backgroundMusicClips.Length == 0)
        {
            if (debugLogs)
                Debug.LogWarning("[SoundColector] Nenhuma música atribuída em backgroundMusicClips.");
            return;
        }

        int index = Random.Range(0, backgroundMusicClips.Length);
        PlayMusic(index, loop);
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    #endregion

    #region Infantaria

    public void PlayInfantrySelect()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayInfantrySelect()");
        PlaySFX(GetRandomClip(Infantry_1, Infantry_2, Infantry_3));
    }

    public void PlayInfantryMove()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayInfantryMove()");
        PlaySFX(Infantry_Moving);
    }

    public void PlayInfantryShot()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayInfantryShot()");
        PlaySFX(Infantry_Shot);
    }

    public void PlayInfantryDeath()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayInfantryDeath()");
        PlaySFX(GetRandomClip(InfantryDeath_1, InfantryDeath_2, InfantryDeath_3));
    }

    #endregion

    #region Tanque

    public void PlayTankSelect()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayTankSelect()");
        PlaySFX(GetRandomClip(Tank_1, Tank_2, Tank_3));
    }

    public void PlayTankMove()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayTankMove()");
        // Podes trocar por um clip específico de movimento se quiseres
        PlaySFX(GetRandomClip(Tank_1, Tank_2, Tank_3));
    }

    public void PlayTankShot()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayTankShot()");
        PlaySFX(GetRandomClip(Tank_Fire_1, Tank_Fire_2));
    }

    public void PlayTankDeath()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayTankDeath()");
        // Aqui reutilizamos as explosões de edifício para a destruição do tanque
        PlaySFX(GetRandomClip(Building_Exp_1, Building_Exp_2));
    }

    #endregion

    #region Edifícios

    public void PlayBuildingHit()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayBuildingHit()");
        PlaySFX(GetRandomClip(Building_Exp_1, Building_Exp_2));
    }

    public void PlayBuildingDeteriorating()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayBuildingDeteriorating()");
        PlaySFX(GetRandomClip(Building_Exp_1, Building_Exp_2));
    }

    public void PlayBuildingDestroyed()
    {
        if (debugLogs) Debug.Log("[SoundColector] PlayBuildingDestroyed()");
        PlaySFX(GetRandomClip(Building_Exp_1, Building_Exp_2));
    }

    #endregion
}
