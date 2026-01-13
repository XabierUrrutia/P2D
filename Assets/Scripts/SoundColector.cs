using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundColector : MonoBehaviour
{
    public static SoundColector Instance { get; private set; }

    public enum MusicState { None, Menu, Gameplay, Pause, Victory, Defeat }
    public enum VoiceLanguage { Auto, Portuguese, English, Spanish }
    public enum VoiceGender { Male, Female }
    public enum VoicePriority { Low, Normal, High, Critical }
    public enum UnitVoiceGenderMode { Fixed, Random50_50 }
    private enum UnitVoiceGroup { Infantry, Tank, Mixed }

    [Header("Voz – Idioma (4 opções)")]
    public VoiceLanguage voiceLanguage = VoiceLanguage.Auto;
    public VoiceLanguage autoFallbackLanguage = VoiceLanguage.Portuguese;

    [Header("Voz – Género das Unidades")]
    public UnitVoiceGenderMode unitVoiceGenderMode = UnitVoiceGenderMode.Random50_50;
    public VoiceGender defaultUnitVoiceGender = VoiceGender.Male;
    [Range(0f, 1f)] public float unitFemaleChance = 0.5f;

    [Header("Voz – AI")]
    public VoiceGender aiVoiceGender = VoiceGender.Female;

    private bool hasLastUnitGender = false;
    private VoiceGender lastUnitVoiceGenderUsed = VoiceGender.Male;

    [Header("Mixer Global (Opcional)")]
    public AudioMixer masterMixer;
    public string musicVolumeParameter = "MusicVolume";
    public string sfxVolumeParameter = "SfxVolume";
    public string voiceVolumeParameter = "VoiceVolume";

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 1f)] public float voiceVolume = 1.0f;
    
    [Header("Mute Global")]
    public bool muteAll = false;

    private const string PrefKey_MusicVolume = "audio_musicVolume";
    private const string PrefKey_SfxVolume = "audio_sfxVolume";
    private const string PrefKey_VoiceVolume = "audio_voiceVolume";
    private const string PrefKey_MuteAll = "audio_muteAll";

    [Header("Voz - Speed (só VOZ)")]
    [Range(0.5f, 2f)] public float voiceSpeed = 1.0f;

    [Header("Voz - Volume por Língua (só VOZ)")]
    public bool useVoiceVolumeByLanguage = true;

    // Preenche no Inspector (PT/EN/ES). O "Auto" é resolvido para a língua efetiva.
    public VoiceLanguageVolume[] voiceVolumeByLanguage = new VoiceLanguageVolume[]
    {
    new VoiceLanguageVolume { language = VoiceLanguage.Portuguese, volume = 1.0f },
    new VoiceLanguageVolume { language = VoiceLanguage.English,    volume = 1.0f },
    new VoiceLanguageVolume { language = VoiceLanguage.Spanish,    volume = 1.0f },
    };

    [Header("Voz - Backstage (Debug)")]
    public bool backstageVoiceVolumeMode = false;
    [Range(0f, 3f)] public float backstageVoiceVolumeMultiplier = 1.0f;

    private int menuEnterCount = 0;
    private AudioClip lastMenuClip = null;

    // ---- Helpers (VOICE only) ----
    private float GetVoiceVolumeForLanguage(VoiceLanguage lang)
    {
        if (!useVoiceVolumeByLanguage || voiceVolumeByLanguage == null || voiceVolumeByLanguage.Length == 0)
            return voiceVolume;

        for (int i = 0; i < voiceVolumeByLanguage.Length; i++)
            if (voiceVolumeByLanguage[i].language == lang)
                return voiceVolumeByLanguage[i].volume;

        return voiceVolume;
    }

    private float GetEffectiveVoiceVolume()
    {
        VoiceLanguage lang = GetEffectiveLanguage();
        float v = GetVoiceVolumeForLanguage(lang);

        if (backstageVoiceVolumeMode)
            v *= backstageVoiceVolumeMultiplier;

        return Mathf.Clamp(v, 0f, 3f);
    }

    private float GetEffectiveVoiceSpeed()
    {
        return Mathf.Clamp(voiceSpeed, 0.5f, 2f);
    }

    [Header("Music Ducking (quando há VO)")]
    public bool enableMusicDucking = true;
    [Range(0f, 0.8f)] public float musicDuckFactor = 0.3f;
    [Range(0.5f, 10f)] public float musicDuckLerpSpeed = 3f;

    [Header("AudioSources (criados em runtime se vazios)")]
    public AudioSource musicSource;
    public AudioSource sfx2DSource;
    public AudioSource voiceSource;

    [Header("Músicas")]
    public AudioClip[] menuMusicClips;
    public AudioClip[] gameplayMusicClips;
    public AudioClip[] pauseMusicClips;
    public AudioClip[] victoryMusicClips;
    public AudioClip[] defeatMusicClips;

    private MusicState currentMusicState = MusicState.None;
    private AudioClip[] currentPlaylist;
    private int currentTrackIndex = -1;
    private bool playlistLoop = true;
    private bool isPlaylistPlaying = false;

    private float targetMusicVolumeFactor = 1f;
    private float lastAnyVoiceTime = -999f;

    [Header("Voz – Anti-caos global")]
    public float globalMinVoiceInterval = 0.35f;

    [System.Serializable]
    public struct VoiceLanguageVolume
    {
        public VoiceLanguage language;
        [Range(0f, 3f)] public float volume;
    }

    [System.Serializable]
    public class LocalizedBalancedVoiceSet
    {
        public AudioClip[] portugueseMale;
        public AudioClip[] portugueseFemale;

        public AudioClip[] englishMale;
        public AudioClip[] englishFemale;

        public AudioClip[] spanishMale;
        public AudioClip[] spanishFemale;

        [HideInInspector] public int[] usagePTMale;
        [HideInInspector] public int[] usagePTFemale;
        [HideInInspector] public int[] usageENMale;
        [HideInInspector] public int[] usageENFemale;
        [HideInInspector] public int[] usageESMale;
        [HideInInspector] public int[] usageESFemale;

        public AudioClip GetNext(VoiceLanguage lang, VoiceGender gender)
        {
            if (lang == VoiceLanguage.Auto)
                lang = VoiceLanguage.Portuguese;

            AudioClip[] pool = null;
            int[] usage = null;

            switch (lang)
            {
                case VoiceLanguage.Portuguese:
                    if (gender == VoiceGender.Female) { pool = portugueseFemale; usage = usagePTFemale; }
                    else { pool = portugueseMale; usage = usagePTMale; }
                    break;

                case VoiceLanguage.English:
                    if (gender == VoiceGender.Female) { pool = englishFemale; usage = usageENFemale; }
                    else { pool = englishMale; usage = usageENMale; }
                    break;

                case VoiceLanguage.Spanish:
                    if (gender == VoiceGender.Female) { pool = spanishFemale; usage = usageESFemale; }
                    else { pool = spanishMale; usage = usageESMale; }
                    break;
            }

            if (pool == null || pool.Length == 0 || AllNull(pool))
            {
                gender = (gender == VoiceGender.Female) ? VoiceGender.Male : VoiceGender.Female;

                switch (lang)
                {
                    case VoiceLanguage.Portuguese:
                        pool = (gender == VoiceGender.Female) ? portugueseFemale : portugueseMale;
                        usage = (gender == VoiceGender.Female) ? usagePTFemale : usagePTMale;
                        break;
                    case VoiceLanguage.English:
                        pool = (gender == VoiceGender.Female) ? englishFemale : englishMale;
                        usage = (gender == VoiceGender.Female) ? usageENFemale : usageENMale;
                        break;
                    case VoiceLanguage.Spanish:
                        pool = (gender == VoiceGender.Female) ? spanishFemale : spanishMale;
                        usage = (gender == VoiceGender.Female) ? usageESFemale : usageESMale;
                        break;
                }
            }

            if (pool == null || pool.Length == 0 || AllNull(pool))
                return null;

            if (usage == null || usage.Length != pool.Length)
                usage = new int[pool.Length];

            List<int> valid = new List<int>();
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null) valid.Add(i);

            if (valid.Count == 0)
                return null;

            int min = int.MaxValue;
            foreach (int idx in valid)
                if (usage[idx] < min) min = usage[idx];

            List<int> candidates = new List<int>();
            foreach (int idx in valid)
                if (usage[idx] == min) candidates.Add(idx);

            int chosen = candidates[Random.Range(0, candidates.Count)];
            usage[chosen]++;

            switch (lang)
            {
                case VoiceLanguage.Portuguese:
                    if (gender == VoiceGender.Female) usagePTFemale = usage;
                    else usagePTMale = usage;
                    break;
                case VoiceLanguage.English:
                    if (gender == VoiceGender.Female) usageENFemale = usage;
                    else usageENMale = usage;
                    break;
                case VoiceLanguage.Spanish:
                    if (gender == VoiceGender.Female) usageESFemale = usage;
                    else usageESMale = usage;
                    break;
            }

            return pool[chosen];
        }

        private bool AllNull(AudioClip[] arr)
        {
            if (arr == null) return true;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != null) return false;
            return true;
        }
    }

    [System.Serializable]
    public class VoiceEventConfig
    {
        public float cooldown = 1.0f;
        public VoicePriority priority = VoicePriority.Normal;
        [HideInInspector] public float lastPlayTime = -999f;
    }

    [Header("Vozes – Unidades (Infantry)")]
    public LocalizedBalancedVoiceSet unitSelectSingleVoices;
    public LocalizedBalancedVoiceSet unitSelectGroupSmallVoices;
    public LocalizedBalancedVoiceSet unitSelectGroupLargeVoices;
    public LocalizedBalancedVoiceSet unitMoveVoices;
    public LocalizedBalancedVoiceSet unitAttackVoices;
    public LocalizedBalancedVoiceSet unitDeathVoices;
    public LocalizedBalancedVoiceSet unitEasterEggVoices;

    [Header("Vozes – Tanks")]
    public LocalizedBalancedVoiceSet tankSelectSingleVoices;
    public LocalizedBalancedVoiceSet tankSelectGroupSmallVoices;
    public LocalizedBalancedVoiceSet tankSelectGroupLargeVoices;
    public LocalizedBalancedVoiceSet tankMoveVoices;
    public LocalizedBalancedVoiceSet tankAttackVoices;
    public LocalizedBalancedVoiceSet tankDeathVoices;

    [Header("Vozes – Grupo Misto (Infantry + Tank)")]
    public LocalizedBalancedVoiceSet mixedSelectGroupSmallVoices;
    public LocalizedBalancedVoiceSet mixedSelectGroupLargeVoices;
    public LocalizedBalancedVoiceSet mixedMoveVoices;
    public LocalizedBalancedVoiceSet mixedAttackVoices;

    [Header("Vozes – Alertas (AI)")]
    public LocalizedBalancedVoiceSet baseUnderAttackVoices;
    public LocalizedBalancedVoiceSet lowResourcesAlertVoices;
    public LocalizedBalancedVoiceSet lowPowerAlertVoices;
    public LocalizedBalancedVoiceSet unitUnderAttackAlertVoices;
    public LocalizedBalancedVoiceSet buildingCapturedVoices;
    public LocalizedBalancedVoiceSet buildingLostVoices;
    public LocalizedBalancedVoiceSet techLevelUpVoices;

    [Header("Vozes – Eventos")]
    public LocalizedBalancedVoiceSet medikitPickupVoices;
    public LocalizedBalancedVoiceSet unitUpgradeVoices;
    public LocalizedBalancedVoiceSet buildingCaptureStartVoices;
    public LocalizedBalancedVoiceSet buildingCaptureCompleteVoices;
    public LocalizedBalancedVoiceSet buildingCaptureFailVoices;
    public LocalizedBalancedVoiceSet insufficientResourcesVoices;
    public LocalizedBalancedVoiceSet invalidCommandVoices;

    [Header("Configs de Voz")]
    public VoiceEventConfig unitSelectionConfig = new VoiceEventConfig { cooldown = 0.3f, priority = VoicePriority.Low };
    public VoiceEventConfig unitMoveConfig = new VoiceEventConfig { cooldown = 0.7f, priority = VoicePriority.Normal };
    public VoiceEventConfig unitAttackConfig = new VoiceEventConfig { cooldown = 1.0f, priority = VoicePriority.Normal };
    public VoiceEventConfig unitDeathVoiceConfig = new VoiceEventConfig { cooldown = 2.0f, priority = VoicePriority.Normal };
    public VoiceEventConfig unitEasterEggConfig = new VoiceEventConfig { cooldown = 3.0f, priority = VoicePriority.Low };

    public VoiceEventConfig baseUnderAttackConfig = new VoiceEventConfig { cooldown = 10f, priority = VoicePriority.High };
    public VoiceEventConfig lowResourcesConfig = new VoiceEventConfig { cooldown = 20f, priority = VoicePriority.Normal };
    public VoiceEventConfig lowPowerConfig = new VoiceEventConfig { cooldown = 20f, priority = VoicePriority.High };
    public VoiceEventConfig unitUnderAttackConfig = new VoiceEventConfig { cooldown = 6f, priority = VoicePriority.Normal };
    public VoiceEventConfig buildingCapturedConfig = new VoiceEventConfig { cooldown = 4f, priority = VoicePriority.Normal };
    public VoiceEventConfig buildingLostConfig = new VoiceEventConfig { cooldown = 4f, priority = VoicePriority.High };
    public VoiceEventConfig techLevelUpConfig = new VoiceEventConfig { cooldown = 4f, priority = VoicePriority.High };

    public VoiceEventConfig insufficientResourcesConfig = new VoiceEventConfig { cooldown = 3f, priority = VoicePriority.Low };
    public VoiceEventConfig invalidCommandConfig = new VoiceEventConfig { cooldown = 1.5f, priority = VoicePriority.Low };

    [Header("Probabilidades de fala")]
    [Range(0f, 1f)] public float unitSelectionVoiceChance = 0.9f;
    [Range(0f, 1f)] public float unitMoveVoiceChance = 0.6f;
    [Range(0f, 1f)] public float unitAttackVoiceChance = 0.5f;
    [Range(0f, 1f)] public float unitDeathVoiceChance = 0.4f;

    [Header("Falas Especiais – Ordens (Infantry, grupo grande)")]
    public LocalizedBalancedVoiceSet unitMoveSpecialVoices;
    public LocalizedBalancedVoiceSet unitAttackSpecialVoices;
    public int moveSpecialMinGeneralCount = 10;
    public int attackSpecialMinGeneralCount = 10;
    [Range(0f, 1f)] public float moveSpecialChanceWhenReady = 0.3f;
    [Range(0f, 1f)] public float attackSpecialChanceWhenReady = 0.3f;
    public int largeGroupMinUnits = 5;

    private int lastSelectedUnitsCountForVoices = 0;
    private int lastSelectedInfantryCountForVoices = 0;
    private int lastSelectedTankCountForVoices = 0;

    private int moveGeneralVoiceCount = 0;
    private bool moveSpecialReady = false;

    private int attackGeneralVoiceCount = 0;
    private bool attackSpecialReady = false;

    private VoicePriority currentVoicePriority = VoicePriority.Low;

    [Header("Delayed Alerts – Initial Delays")]
    public float unitUnderAttackInitialDelay = 1.2f;
    public float lowResourcesInitialDelay = 1.5f;
    public float lowPowerInitialDelay = 1.5f;

    private bool unitUnderAttackAlertPending = false;
    private Coroutine unitUnderAttackDelayCoroutine = null;

    private bool lowResourcesAlertPending = false;
    private Coroutine lowResourcesDelayCoroutine = null;

    private bool lowPowerAlertPending = false;
    private Coroutine lowPowerDelayCoroutine = null;

    [Header("SFX – Unidades")]
    public AudioClip[] infantryShotClips;
    public AudioClip[] infantryDeathClips;
    public AudioClip[] tankShotClips;
    public AudioClip[] tankDeathClips;

    private int[] infantryShotUsage;
    private int[] infantryDeathUsage;
    private int[] tankShotUsage;
    private int[] tankDeathUsage;

    [Header("SFX – Inimigos (Morte)")]
    public AudioClip[] enemyInfantryDeathClips;
    public AudioClip[] enemyTankDeathClips;
    private int[] enemyInfantryDeathUsage;
    private int[] enemyTankDeathUsage;

    [Header("SFX – Edifícios")]
    public AudioClip[] buildingSelectClips;
    private int[] buildingSelectUsage;

    public AudioClip buildingCaptureStartSfx;
    public AudioClip buildingCaptureCompleteSfx;
    public AudioClip buildingCaptureFailSfx;

    public AudioClip[] buildingDestroyedClips;
    private int[] buildingDestroyedUsage;

    [Header("SFX – UI")]
    public AudioClip uiClickClip;
    public AudioClip uiPanelOpenClip;

    [Header("Config SFX")]
    public bool randomizeSfxPitch = true;
    [Range(0.8f, 1.2f)] public float minSfxPitch = 0.95f;
    [Range(0.8f, 1.2f)] public float maxSfxPitch = 1.05f;

    [Header("Debug")]
    public bool debugLogs = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadAudioSettings();
        SetupAudioSources();
        ApplyAIGenderRule();
        ApplyMixerVolumes();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyAIGenderRule();
    }
#endif

    private void OnEnable()
    {
        GameEvents.OnUnitsSelected += HandleUnitsSelected;
        GameEvents.OnBaseUnderAttack += HandleBaseUnderAttack;
        GameEvents.OnLowResources += HandleLowResources;
        GameEvents.OnLowPower += HandleLowPower;
        GameEvents.OnUnitUnderAttack += HandleUnitUnderAttack;

        GameEvents.OnBuildingSelected += HandleBuildingSelected;
        GameEvents.OnBuildingCaptured += HandleBuildingCaptured;
        GameEvents.OnBuildingLost += HandleBuildingLost;
        GameEvents.OnBuildingCaptureStarted += HandleBuildingCaptureStarted;
        GameEvents.OnBuildingCaptureCompleted += HandleBuildingCaptureCompleted;
        GameEvents.OnBuildingCaptureFailed += HandleBuildingCaptureFailed;

        GameEvents.OnMedikitPickedUp += HandleMedikitPickedUp;
        GameEvents.OnUnitUpgraded += HandleUnitUpgraded;

        GameEvents.OnTechLevelUp += HandleTechLevelUp;
        GameEvents.OnInsufficientResources += HandleInsufficientResources;
        GameEvents.OnInvalidCommand += HandleInvalidCommand;
    }

    private void OnDisable()
    {
        GameEvents.OnUnitsSelected -= HandleUnitsSelected;
        GameEvents.OnBaseUnderAttack -= HandleBaseUnderAttack;
        GameEvents.OnLowResources -= HandleLowResources;
        GameEvents.OnLowPower -= HandleLowPower;
        GameEvents.OnUnitUnderAttack -= HandleUnitUnderAttack;

        GameEvents.OnBuildingSelected -= HandleBuildingSelected;
        GameEvents.OnBuildingCaptured -= HandleBuildingCaptured;
        GameEvents.OnBuildingLost -= HandleBuildingLost;
        GameEvents.OnBuildingCaptureStarted -= HandleBuildingCaptureStarted;
        GameEvents.OnBuildingCaptureCompleted -= HandleBuildingCaptureCompleted;
        GameEvents.OnBuildingCaptureFailed -= HandleBuildingCaptureFailed;

        GameEvents.OnMedikitPickedUp -= HandleMedikitPickedUp;
        GameEvents.OnUnitUpgraded -= HandleUnitUpgraded;

        GameEvents.OnTechLevelUp -= HandleTechLevelUp;
        GameEvents.OnInsufficientResources -= HandleInsufficientResources;
        GameEvents.OnInvalidCommand -= HandleInvalidCommand;

        if (unitUnderAttackDelayCoroutine != null) { StopCoroutine(unitUnderAttackDelayCoroutine); unitUnderAttackDelayCoroutine = null; }
        unitUnderAttackAlertPending = false;

        if (lowResourcesDelayCoroutine != null) { StopCoroutine(lowResourcesDelayCoroutine); lowResourcesDelayCoroutine = null; }
        lowResourcesAlertPending = false;

        if (lowPowerDelayCoroutine != null) { StopCoroutine(lowPowerDelayCoroutine); lowPowerDelayCoroutine = null; }
        lowPowerAlertPending = false;
    }

    private void Update()
    {
        UpdateMusicDucking();

        if (isPlaylistPlaying && musicSource != null && !musicSource.isPlaying)
            PlayNextTrackInPlaylist();
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            var go = new GameObject("MusicSource");
            go.transform.SetParent(transform);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.playOnAwake = false;
            musicSource.loop = false;
        }

        if (sfx2DSource == null)
        {
            var go = new GameObject("SFX2DSource");
            go.transform.SetParent(transform);
            sfx2DSource = go.AddComponent<AudioSource>();
            sfx2DSource.spatialBlend = 0f;
            sfx2DSource.playOnAwake = false;
            sfx2DSource.loop = false;
        }

        if (voiceSource == null)
        {
            var go = new GameObject("VoiceSource");
            go.transform.SetParent(transform);
            voiceSource = go.AddComponent<AudioSource>();
            voiceSource.spatialBlend = 0f;
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.pitch = GetEffectiveVoiceSpeed();
            voiceSource.volume = GetEffectiveVoiceVolume();
        }
    }

    private void ApplyMixerVolumes()
    {
        float musicVol = muteAll ? 0f : musicVolume;
        float sfxVol = muteAll ? 0f : sfxVolume;
        float voiceVolLin = muteAll ? 0f : GetEffectiveVoiceVolume();

        if (masterMixer != null)
        {
            if (!string.IsNullOrEmpty(musicVolumeParameter))
                masterMixer.SetFloat(musicVolumeParameter,
                    Mathf.Log10(Mathf.Clamp(musicVol <= 0f ? 0.0001f : musicVol, 0.0001f, 1f)) * 20f);

            if (!string.IsNullOrEmpty(sfxVolumeParameter))
                masterMixer.SetFloat(sfxVolumeParameter,
                    Mathf.Log10(Mathf.Clamp(sfxVol <= 0f ? 0.0001f : sfxVol, 0.0001f, 1f)) * 20f);

            if (!string.IsNullOrEmpty(voiceVolumeParameter))
                masterMixer.SetFloat(voiceVolumeParameter,
                    Mathf.Log10(Mathf.Clamp(voiceVolLin <= 0f ? 0.0001f : voiceVolLin, 0.0001f, 3f)) * 20f);
        }

        if (musicSource != null) musicSource.volume = musicVol * targetMusicVolumeFactor;
        if (sfx2DSource != null) sfx2DSource.volume = sfxVol;
        if (voiceSource != null)
        {
            voiceSource.volume = voiceVolLin;
            voiceSource.pitch = GetEffectiveVoiceSpeed();
        }

        SaveAudioSettings();
    }

    // -------- Integração com menu de opções (sliders) --------
    /// <summary>
    /// Define o volume da música (0-1) e aplica no AudioMixer.
    /// Pode ser chamado a partir de sliders de opções.
    /// </summary>
    public void SetMusicVolume01(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyMixerVolumes();
    }

    /// <summary>
    /// Define o volume de SFX (0-1) e aplica no AudioMixer.
    /// </summary>
    public void SetSfxVolume01(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyMixerVolumes();
    }

    /// <summary>
    /// Define o volume de voz (0-1) e aplica no AudioMixer.
    /// </summary>
    public void SetVoiceVolume01(float value)
    {
        voiceVolume = Mathf.Clamp01(value);
        ApplyMixerVolumes();
    }
    public void SetMuteAll(bool muted)
    {
        muteAll = muted;
        ApplyMixerVolumes();
    }
    /// <summary>
    /// Define um volume global simples (0-1) para música e SFX ao mesmo tempo.
    /// Útil se tiveres só um slider de volume geral nas opções.
    /// </summary>
    public void SetMasterLikeVolume01(float value)
    {
        float v = Mathf.Clamp01(value);
        musicVolume = v;
        sfxVolume = v;
        // se quiseres também afetar voz, descomenta:
        // voiceVolume = v;
        ApplyMixerVolumes();
    }
    private void LoadAudioSettings()
    {
        if (PlayerPrefs.HasKey(PrefKey_MusicVolume))
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKey_MusicVolume, musicVolume));

        if (PlayerPrefs.HasKey(PrefKey_SfxVolume))
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKey_SfxVolume, sfxVolume));

        if (PlayerPrefs.HasKey(PrefKey_VoiceVolume))
            voiceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKey_VoiceVolume, voiceVolume));

        muteAll = PlayerPrefs.GetInt(PrefKey_MuteAll, 0) == 1;
    }

    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat(PrefKey_MusicVolume, musicVolume);
        PlayerPrefs.SetFloat(PrefKey_SfxVolume, sfxVolume);
        PlayerPrefs.SetFloat(PrefKey_VoiceVolume, voiceVolume);
        PlayerPrefs.SetInt(PrefKey_MuteAll, muteAll ? 1 : 0);
        PlayerPrefs.Save();
    }
    // -------- Backstage controls (code-only) --------
    public void SetVoiceSpeed(float speed)
    {
        voiceSpeed = Mathf.Clamp(speed, 0.5f, 2f);
        if (voiceSource != null) voiceSource.pitch = GetEffectiveVoiceSpeed();
    }

    public void EnableBackstageVoiceVolumeMode(bool enabled)
    {
        backstageVoiceVolumeMode = enabled;
        ApplyMixerVolumes();
    }

    public void SetBackstageVoiceVolumeMultiplier(float multiplier)
    {
        backstageVoiceVolumeMultiplier = Mathf.Clamp(multiplier, 0f, 3f);
        if (backstageVoiceVolumeMode) ApplyMixerVolumes();
    }
    // -----------------------------------------------

    private void StartMenuTrackState(AudioClip[] clips, bool loop)
    {
        isPlaylistPlaying = false;
        currentPlaylist = null;

        if (clips == null || clips.Length == 0) { StopMusic(); return; }

        AudioClip clip;

        // 1º menu: fixa na primeira faixa válida do array (não random)
        if (menuEnterCount == 0)
        {
            clip = FirstNonNull(clips);
            if (clip == null) clip = clips[Random.Range(0, clips.Length)];
        }
        // a partir do 2º menu: random, evitando repetir a anterior
        else
        {
            clip = RandomNonNullExcluding(clips, lastMenuClip);
        }

        if (musicSource == null || clip == null) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume * targetMusicVolumeFactor;
        musicSource.Play();

        lastMenuClip = clip;
        menuEnterCount++;
    }

    private AudioClip FirstNonNull(AudioClip[] clips)
    {
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] != null) return clips[i];
        return null;
    }

    private AudioClip RandomNonNullExcluding(AudioClip[] clips, AudioClip exclude)
    {
        if (clips == null || clips.Length == 0) return null;

        AudioClip candidate = null;
        int safety = 30;

        while (safety-- > 0)
        {
            candidate = clips[Random.Range(0, clips.Length)];
            if (candidate != null && candidate != exclude) return candidate;
        }

        return FirstNonNull(clips);
    }

    private VoiceLanguage GetEffectiveLanguage()
    {
        if (voiceLanguage != VoiceLanguage.Auto)
            return voiceLanguage;

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Portuguese: return VoiceLanguage.Portuguese;
            case SystemLanguage.Spanish: return VoiceLanguage.Spanish;
            case SystemLanguage.English: return VoiceLanguage.English;
            default:
                return (autoFallbackLanguage == VoiceLanguage.Auto) ? VoiceLanguage.Portuguese : autoFallbackLanguage;
        }
    }

    private VoiceGender GetUnitGenderForThisVoice()
    {
        if (unitVoiceGenderMode == UnitVoiceGenderMode.Fixed)
            return defaultUnitVoiceGender;

        return (Random.value < unitFemaleChance) ? VoiceGender.Female : VoiceGender.Male;
    }

    private static VoiceGender Opposite(VoiceGender g) => (g == VoiceGender.Male) ? VoiceGender.Female : VoiceGender.Male;

    private VoiceGender GetAIGenderForThisVoice()
    {
        if (hasLastUnitGender)
            return Opposite(lastUnitVoiceGenderUsed);
        return Opposite(defaultUnitVoiceGender);
    }

    private void RegisterUnitGender(VoiceGender g)
    {
        hasLastUnitGender = true;
        lastUnitVoiceGenderUsed = g;
        aiVoiceGender = Opposite(g);
    }

    private void ApplyAIGenderRule()
    {
        aiVoiceGender = Opposite(defaultUnitVoiceGender);
    }

    public void PlayMenuMusic() => SetMusicState(MusicState.Menu);
    public void PlayGameplayMusic() => SetMusicState(MusicState.Gameplay);
    public void PlayPauseMusic() => SetMusicState(MusicState.Pause);
    public void PlayVictoryMusic() => SetMusicState(MusicState.Victory);
    public void PlayDefeatMusic() => SetMusicState(MusicState.Defeat);

    public void StopMusic()
    {
        isPlaylistPlaying = false;
        if (musicSource != null) musicSource.Stop();
    }

    private void SetMusicState(MusicState newState)
    {
        currentMusicState = newState;

        switch (newState)
        {
            case MusicState.Menu:
                StartMenuTrackState(menuMusicClips, true);
                break;
            case MusicState.Gameplay:
                StartPlaylist(gameplayMusicClips, true);
                break;
            case MusicState.Pause:
                StartSingleTrackState(pauseMusicClips, true);
                break;
            case MusicState.Victory:
                StartSingleTrackState(victoryMusicClips, false);
                break;
            case MusicState.Defeat:
                StartSingleTrackState(defeatMusicClips, false);
                break;
            default:
                StopMusic();
                break;
        }
    }

    private void StartSingleTrackState(AudioClip[] clips, bool loop)
    {
        isPlaylistPlaying = false;
        currentPlaylist = null;

        if (clips == null || clips.Length == 0) { StopMusic(); return; }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (musicSource == null || clip == null) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume * targetMusicVolumeFactor;
        musicSource.Play();
    }

    private void StartPlaylist(AudioClip[] playlist, bool loop)
    {
        if (playlist == null || playlist.Length == 0) { StopMusic(); return; }

        currentPlaylist = playlist;
        playlistLoop = loop;
        currentTrackIndex = -1;
        isPlaylistPlaying = true;

        PlayNextTrackInPlaylist();
    }

    private void PlayNextTrackInPlaylist()
    {
        if (currentPlaylist == null || currentPlaylist.Length == 0 || musicSource == null)
            return;

        currentTrackIndex++;
        if (currentTrackIndex >= currentPlaylist.Length)
        {
            if (playlistLoop) currentTrackIndex = 0;
            else { isPlaylistPlaying = false; return; }
        }

        AudioClip clip = currentPlaylist[currentTrackIndex];
        if (clip == null) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = false;
        musicSource.volume = musicVolume * targetMusicVolumeFactor;
        musicSource.Play();
    }

    private void UpdateMusicDucking()
    {
        if (!enableMusicDucking || musicSource == null || voiceSource == null)
            return;

        float target = voiceSource.isPlaying ? (1f - musicDuckFactor) : 1f;

        targetMusicVolumeFactor = Mathf.Lerp(
            targetMusicVolumeFactor,
            target,
            Time.deltaTime * musicDuckLerpSpeed
        );

        musicSource.volume = musicVolume * targetMusicVolumeFactor;
    }

    private void PlaySfx2D(AudioClip clip, float volumeMul = 1f)
    {
        if (clip == null || sfx2DSource == null) return;

        sfx2DSource.pitch = randomizeSfxPitch ? Random.Range(minSfxPitch, maxSfxPitch) : 1f;
        sfx2DSource.PlayOneShot(clip, sfxVolume * volumeMul);
    }

    private void PlayWorldSfx3D(AudioClip clip, Vector3 worldPos, float volumeMul = 1f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("SFX3D_" + clip.name);
        go.transform.position = worldPos;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 2f;
        src.maxDistance = 35f;
        src.playOnAwake = false;
        src.loop = false;
        src.volume = sfxVolume * volumeMul;

        if (randomizeSfxPitch)
            src.pitch = Random.Range(minSfxPitch, maxSfxPitch);

        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    private AudioClip GetBalancedClip(AudioClip[] pool, ref int[] usage)
    {
        if (pool == null || pool.Length == 0) return null;

        if (usage == null || usage.Length != pool.Length)
            usage = new int[pool.Length];

        List<int> valid = new List<int>();
        for (int i = 0; i < pool.Length; i++)
            if (pool[i] != null) valid.Add(i);

        if (valid.Count == 0)
            return null;

        int min = int.MaxValue;
        foreach (int idx in valid)
            if (usage[idx] < min) min = usage[idx];

        List<int> candidates = new List<int>();
        foreach (int idx in valid)
            if (usage[idx] == min) candidates.Add(idx);

        int chosen = candidates[Random.Range(0, candidates.Count)];
        usage[chosen]++;
        return pool[chosen];
    }

    private bool PlayVoiceWithConfig(LocalizedBalancedVoiceSet set, VoiceEventConfig config, VoiceGender gender)
    {
        if (set == null || voiceSource == null) return false;

        float now = Time.time;

        if (now - lastAnyVoiceTime < globalMinVoiceInterval)
            return false;

        if (config != null && now - config.lastPlayTime < config.cooldown)
            return false;

        AudioClip clip = set.GetNext(GetEffectiveLanguage(), gender);
        if (clip == null) return false;

        VoicePriority priority = (config != null) ? config.priority : VoicePriority.Normal;

        if (voiceSource.isPlaying)
        {
            if (priority < currentVoicePriority)
                return false;
            voiceSource.Stop();
        }

        currentVoicePriority = priority;

        voiceSource.pitch = GetEffectiveVoiceSpeed();
        voiceSource.clip = clip;
        voiceSource.volume = GetEffectiveVoiceVolume();
        voiceSource.Play();

        lastAnyVoiceTime = now;
        if (config != null) config.lastPlayTime = now;

        return true;
    }

    private bool PlayVoiceWithConfig(LocalizedBalancedVoiceSet set, VoiceEventConfig config)
    {
        return PlayVoiceWithConfig(set, config, GetAIGenderForThisVoice());
    }

    private bool PlayUnitVoiceWithConfig(LocalizedBalancedVoiceSet set, VoiceEventConfig config, VoiceGender gender)
    {
        bool played = PlayVoiceWithConfig(set, config, gender);
        if (played) RegisterUnitGender(gender);
        return played;
    }

    private UnitVoiceGroup GetVoiceGroup(int infantryCount, int tankCount)
    {
        if (tankCount > 0 && infantryCount > 0) return UnitVoiceGroup.Mixed;
        if (tankCount > 0) return UnitVoiceGroup.Tank;
        return UnitVoiceGroup.Infantry;
    }

    private void CacheSelectionComposition(int infantryCount, int tankCount)
    {
        lastSelectedInfantryCountForVoices = Mathf.Max(0, infantryCount);
        lastSelectedTankCountForVoices = Mathf.Max(0, tankCount);
        lastSelectedUnitsCountForVoices = lastSelectedInfantryCountForVoices + lastSelectedTankCountForVoices;
    }

    private LocalizedBalancedVoiceSet GetSelectionSet(UnitVoiceGroup group, int totalCount)
    {
        if (group == UnitVoiceGroup.Tank)
        {
            if (totalCount <= 1 && tankSelectSingleVoices != null) return tankSelectSingleVoices;
            if (totalCount <= 4 && tankSelectGroupSmallVoices != null) return tankSelectGroupSmallVoices;
            if (tankSelectGroupLargeVoices != null) return tankSelectGroupLargeVoices;
        }

        if (group == UnitVoiceGroup.Mixed)
        {
            if (totalCount <= 4 && mixedSelectGroupSmallVoices != null) return mixedSelectGroupSmallVoices;
            if (mixedSelectGroupLargeVoices != null) return mixedSelectGroupLargeVoices;
        }

        if (totalCount <= 1) return unitSelectSingleVoices;
        if (totalCount <= 4) return unitSelectGroupSmallVoices;
        return unitSelectGroupLargeVoices;
    }

    private LocalizedBalancedVoiceSet GetMoveSet(UnitVoiceGroup group)
    {
        if (group == UnitVoiceGroup.Tank && tankMoveVoices != null) return tankMoveVoices;
        if (group == UnitVoiceGroup.Mixed && mixedMoveVoices != null) return mixedMoveVoices;
        return unitMoveVoices;
    }

    private LocalizedBalancedVoiceSet GetAttackSet(UnitVoiceGroup group)
    {
        if (group == UnitVoiceGroup.Tank && tankAttackVoices != null) return tankAttackVoices;
        if (group == UnitVoiceGroup.Mixed && mixedAttackVoices != null) return mixedAttackVoices;
        return unitAttackVoices;
    }

    private LocalizedBalancedVoiceSet GetDeathVoiceSet(bool isTank)
    {
        if (isTank && tankDeathVoices != null) return tankDeathVoices;
        return unitDeathVoices;
    }

    public void PlayInfantryShotAt(Vector3 pos)
    {
        AudioClip c = GetBalancedClip(infantryShotClips, ref infantryShotUsage);
        PlayWorldSfx3D(c, pos);
    }

    public void PlayInfantryDeathAt(Vector3 pos)
    {
        AudioClip c = GetBalancedClip(infantryDeathClips, ref infantryDeathUsage);
        PlayWorldSfx3D(c, pos);
    }

    public void PlayTankShotAt(Vector3 pos)
    {
        AudioClip c = GetBalancedClip(tankShotClips, ref tankShotUsage);
        PlayWorldSfx3D(c, pos);
    }

    public void PlayTankDeathAt(Vector3 pos)
    {
        AudioClip c = GetBalancedClip(tankDeathClips, ref tankDeathUsage);
        PlayWorldSfx3D(c, pos);
    }

    public void PlayEnemyInfantryDeath()
    {
        AudioClip c = GetBalancedClip(enemyInfantryDeathClips, ref enemyInfantryDeathUsage);
        PlaySfx2D(c);
    }

    public void PlayEnemyInfantryDeathAt(Vector3 pos)
    {
        AudioClip c = GetBalancedClip(enemyInfantryDeathClips, ref enemyInfantryDeathUsage);
        PlayWorldSfx3D(c, pos);
    }

    public void PlayEnemyTankDeath()
    {
        AudioClip c = GetBalancedClip(enemyTankDeathClips, ref enemyTankDeathUsage);
        PlaySfx2D(c);
    }

    public void PlayEnemyTankDeathAt(Vector3 pos)
    {
        AudioClip c = GetBalancedClip(enemyTankDeathClips, ref enemyTankDeathUsage);
        PlayWorldSfx3D(c, pos);
    }

    public void PlayUnitSelectionVoice(int count, VoiceGender gender)
    {
        PlayUnitSelectionVoice(count, 0, gender);
    }

    public void PlayUnitSelectionVoice(int count)
    {
        PlayUnitSelectionVoice(count, 0, GetUnitGenderForThisVoice());
    }

    public void PlayUnitSelectionVoice(int infantryCount, int tankCount)
    {
        PlayUnitSelectionVoice(infantryCount, tankCount, GetUnitGenderForThisVoice());
    }

    public void PlayUnitSelectionVoice(int infantryCount, int tankCount, VoiceGender gender)
    {
        int total = Mathf.Max(0, infantryCount) + Mathf.Max(0, tankCount);
        if (total <= 0) return;

        if (Random.value > unitSelectionVoiceChance)
            return;

        CacheSelectionComposition(infantryCount, tankCount);

        var group = GetVoiceGroup(infantryCount, tankCount);
        var set = GetSelectionSet(group, total);

        PlayUnitVoiceWithConfig(set, unitSelectionConfig, gender);
    }

    public void PlayUnitMoveVoice(int count, VoiceGender gender)
    {
        CacheSelectionComposition(count, 0);
        bool isLargeGroup = count >= largeGroupMinUnits;

        if (isLargeGroup && unitMoveSpecialVoices != null && moveSpecialMinGeneralCount > 0)
        {
            if (moveSpecialReady && Random.value <= moveSpecialChanceWhenReady)
            {
                bool playedSpecial = PlayUnitVoiceWithConfig(unitMoveSpecialVoices, unitMoveConfig, gender);
                if (playedSpecial)
                {
                    moveSpecialReady = false;
                    moveGeneralVoiceCount = 0;
                    return;
                }
            }
        }

        if (Random.value > unitMoveVoiceChance)
            return;

        bool played = PlayUnitVoiceWithConfig(unitMoveVoices, unitMoveConfig, gender);
        if (played)
        {
            if (!moveSpecialReady)
            {
                moveGeneralVoiceCount++;
                if (moveGeneralVoiceCount >= moveSpecialMinGeneralCount)
                    moveSpecialReady = true;
            }
        }
    }

    public void PlayUnitMoveVoice()
    {
        if (lastSelectedInfantryCountForVoices > 0 || lastSelectedTankCountForVoices > 0)
            PlayUnitMoveVoice(lastSelectedInfantryCountForVoices, lastSelectedTankCountForVoices, GetUnitGenderForThisVoice());
        else
            PlayUnitMoveVoice(lastSelectedUnitsCountForVoices, GetUnitGenderForThisVoice());
    }

    public void PlayUnitMoveVoice(VoiceGender gender)
    {
        if (lastSelectedInfantryCountForVoices > 0 || lastSelectedTankCountForVoices > 0)
            PlayUnitMoveVoice(lastSelectedInfantryCountForVoices, lastSelectedTankCountForVoices, gender);
        else
            PlayUnitMoveVoice(lastSelectedUnitsCountForVoices, gender);
    }

    public void PlayUnitMoveVoice(int infantryCount, int tankCount)
    {
        PlayUnitMoveVoice(infantryCount, tankCount, GetUnitGenderForThisVoice());
    }

    public void PlayUnitMoveVoice(int infantryCount, int tankCount, VoiceGender gender)
    {
        int total = Mathf.Max(0, infantryCount) + Mathf.Max(0, tankCount);
        if (total <= 0) return;

        CacheSelectionComposition(infantryCount, tankCount);

        var group = GetVoiceGroup(infantryCount, tankCount);

        if (group == UnitVoiceGroup.Infantry)
        {
            PlayUnitMoveVoice(total, gender);
            return;
        }

        if (Random.value > unitMoveVoiceChance)
            return;

        PlayUnitVoiceWithConfig(GetMoveSet(group), unitMoveConfig, gender);
    }

    public void PlayUnitAttackVoice(int count, VoiceGender gender)
    {
        CacheSelectionComposition(count, 0);
        bool isLargeGroup = count >= largeGroupMinUnits;

        if (isLargeGroup && unitAttackSpecialVoices != null && attackSpecialMinGeneralCount > 0)
        {
            if (attackSpecialReady && Random.value <= attackSpecialChanceWhenReady)
            {
                bool playedSpecial = PlayUnitVoiceWithConfig(unitAttackSpecialVoices, unitAttackConfig, gender);
                if (playedSpecial)
                {
                    attackSpecialReady = false;
                    attackGeneralVoiceCount = 0;
                    return;
                }
            }
        }

        if (Random.value > unitAttackVoiceChance)
            return;

        bool played = PlayUnitVoiceWithConfig(unitAttackVoices, unitAttackConfig, gender);
        if (played)
        {
            if (!attackSpecialReady)
            {
                attackGeneralVoiceCount++;
                if (attackGeneralVoiceCount >= attackSpecialMinGeneralCount)
                    attackSpecialReady = true;
            }
        }
    }

    public void PlayUnitAttackVoice()
    {
        if (lastSelectedInfantryCountForVoices > 0 || lastSelectedTankCountForVoices > 0)
            PlayUnitAttackVoice(lastSelectedInfantryCountForVoices, lastSelectedTankCountForVoices, GetUnitGenderForThisVoice());
        else
            PlayUnitAttackVoice(lastSelectedUnitsCountForVoices, GetUnitGenderForThisVoice());
    }

    public void PlayUnitAttackVoice(VoiceGender gender)
    {
        if (lastSelectedInfantryCountForVoices > 0 || lastSelectedTankCountForVoices > 0)
            PlayUnitAttackVoice(lastSelectedInfantryCountForVoices, lastSelectedTankCountForVoices, gender);
        else
            PlayUnitAttackVoice(lastSelectedUnitsCountForVoices, gender);
    }

    public void PlayUnitAttackVoice(int infantryCount, int tankCount)
    {
        PlayUnitAttackVoice(infantryCount, tankCount, GetUnitGenderForThisVoice());
    }

    public void PlayUnitAttackVoice(int infantryCount, int tankCount, VoiceGender gender)
    {
        int total = Mathf.Max(0, infantryCount) + Mathf.Max(0, tankCount);
        if (total <= 0) return;

        CacheSelectionComposition(infantryCount, tankCount);

        var group = GetVoiceGroup(infantryCount, tankCount);

        if (group == UnitVoiceGroup.Infantry)
        {
            PlayUnitAttackVoice(total, gender);
            return;
        }

        if (Random.value > unitAttackVoiceChance)
            return;

        PlayUnitVoiceWithConfig(GetAttackSet(group), unitAttackConfig, gender);
    }

    public void PlayUnitDeathVoice(VoiceGender gender)
    {
        if (Random.value > unitDeathVoiceChance)
            return;

        PlayUnitVoiceWithConfig(unitDeathVoices, unitDeathVoiceConfig, gender);
    }

    public void PlayUnitDeathVoice()
    {
        PlayUnitDeathVoice(GetUnitGenderForThisVoice());
    }

    public void PlayUnitDeathVoice(bool isTank, VoiceGender gender)
    {
        if (Random.value > unitDeathVoiceChance)
            return;

        PlayUnitVoiceWithConfig(GetDeathVoiceSet(isTank), unitDeathVoiceConfig, gender);
    }

    public void PlayUnitDeathVoice(bool isTank)
    {
        PlayUnitDeathVoice(isTank, GetUnitGenderForThisVoice());
    }

    public void PlayUnitEasterEggVoice(VoiceGender gender)
    {
        PlayUnitVoiceWithConfig(unitEasterEggVoices, unitEasterEggConfig, gender);
    }

    public void PlayUnitEasterEggVoice()
    {
        PlayUnitEasterEggVoice(GetUnitGenderForThisVoice());
    }

    public void PlayMedikitPickupVoice(VoiceGender gender)
    {
        PlayUnitVoiceWithConfig(medikitPickupVoices, null, gender);
    }

    public void PlayMedikitPickupVoice()
    {
        PlayMedikitPickupVoice(GetUnitGenderForThisVoice());
    }

    public void PlayUnitUpgradeVoice(VoiceGender gender)
    {
        PlayUnitVoiceWithConfig(unitUpgradeVoices, null, gender);
    }

    public void PlayUnitUpgradeVoice()
    {
        PlayUnitUpgradeVoice(GetUnitGenderForThisVoice());
    }

    public void PlayInsufficientResourcesVoice(VoiceGender gender)
    {
        PlayUnitVoiceWithConfig(insufficientResourcesVoices, insufficientResourcesConfig, gender);
    }

    public void PlayInsufficientResourcesVoice()
    {
        PlayInsufficientResourcesVoice(GetUnitGenderForThisVoice());
    }

    public void PlayInvalidCommandVoice(VoiceGender gender)
    {
        PlayUnitVoiceWithConfig(invalidCommandVoices, invalidCommandConfig, gender);
    }

    public void PlayInvalidCommandVoice()
    {
        PlayInvalidCommandVoice(GetUnitGenderForThisVoice());
    }

    public void PlayBaseUnderAttackVoice()
    {
        PlayVoiceWithConfig(baseUnderAttackVoices, baseUnderAttackConfig);
    }

    public void PlayLowResourcesAlert()
    {
        PlayVoiceWithConfig(lowResourcesAlertVoices, lowResourcesConfig);
    }

    public void PlayLowPowerAlert()
    {
        PlayVoiceWithConfig(lowPowerAlertVoices, lowPowerConfig);
    }

    public void PlayUnitUnderAttackAlert()
    {
        PlayVoiceWithConfig(unitUnderAttackAlertVoices, unitUnderAttackConfig);
    }

    public void PlayBuildingCapturedAlert()
    {
        PlayVoiceWithConfig(buildingCapturedVoices, buildingCapturedConfig);
    }

    public void PlayBuildingLostAlert()
    {
        PlayVoiceWithConfig(buildingLostVoices, buildingLostConfig);
    }

    public void PlayTechLevelUpVoice()
    {
        PlayVoiceWithConfig(techLevelUpVoices, techLevelUpConfig);
    }

    public void PlayBuildingCaptureStartVoice()
    {
        PlayVoiceWithConfig(buildingCaptureStartVoices, null);
    }

    public void PlayBuildingCaptureCompleteVoice()
    {
        PlayVoiceWithConfig(buildingCaptureCompleteVoices, null);
    }

    public void PlayBuildingCaptureFailVoice()
    {
        PlayVoiceWithConfig(buildingCaptureFailVoices, null);
    }

    public void PlayBuildingDestroyedAt(Vector3 pos)
    {
        AudioClip c = GetBalancedClip(buildingDestroyedClips, ref buildingDestroyedUsage);
        PlayWorldSfx3D(c, pos);
    }

    public void PlayBuildingSelect()
    {
        AudioClip c = GetBalancedClip(buildingSelectClips, ref buildingSelectUsage);
        PlaySfx2D(c);
    }

    public void PlayBuildingCaptureStartSfx()
    {
        PlaySfx2D(buildingCaptureStartSfx);
    }

    public void PlayBuildingCaptureCompleteSfx()
    {
        PlaySfx2D(buildingCaptureCompleteSfx);
    }

    public void PlayBuildingCaptureFailSfx()
    {
        PlaySfx2D(buildingCaptureFailSfx);
    }

    public void PlayUiClick()
    {
        PlaySfx2D(uiClickClip);
    }

    public void PlayUiPanelOpen()
    {
        PlaySfx2D(uiPanelOpenClip);
    }

    private IEnumerator UnitUnderAttackDelayRoutine()
    {
        float delay = Mathf.Max(0f, unitUnderAttackInitialDelay);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        PlayUnitUnderAttackAlert();

        unitUnderAttackAlertPending = false;
        unitUnderAttackDelayCoroutine = null;
    }

    private IEnumerator LowResourcesDelayRoutine()
    {
        float delay = Mathf.Max(0f, lowResourcesInitialDelay);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        PlayLowResourcesAlert();

        lowResourcesAlertPending = false;
        lowResourcesDelayCoroutine = null;
    }

    private IEnumerator LowPowerDelayRoutine()
    {
        float delay = Mathf.Max(0f, lowPowerInitialDelay);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        PlayLowPowerAlert();

        lowPowerAlertPending = false;
        lowPowerDelayCoroutine = null;
    }

    private void HandleUnitsSelected(int count)
    {
        PlayUnitSelectionVoice(count);
    }

    private void HandleBaseUnderAttack()
    {
        PlayBaseUnderAttackVoice();
    }

    private void HandleLowResources()
    {
        if (lowResourcesAlertPending) return;
        lowResourcesAlertPending = true;
        lowResourcesDelayCoroutine = StartCoroutine(LowResourcesDelayRoutine());
    }

    private void HandleLowPower()
    {
        if (lowPowerAlertPending) return;
        lowPowerAlertPending = true;
        lowPowerDelayCoroutine = StartCoroutine(LowPowerDelayRoutine());
    }

    private void HandleUnitUnderAttack()
    {
        if (unitUnderAttackAlertPending) return;
        unitUnderAttackAlertPending = true;
        unitUnderAttackDelayCoroutine = StartCoroutine(UnitUnderAttackDelayRoutine());
    }

    private void HandleBuildingSelected()
    {
        PlayBuildingSelect();
    }

    private void HandleBuildingCaptured()
    {
        PlayBuildingCapturedAlert();
    }

    private void HandleBuildingLost()
    {
        PlayBuildingLostAlert();
    }

    private void HandleBuildingCaptureStarted()
    {
        PlayBuildingCaptureStartSfx();
    }

    private void HandleBuildingCaptureCompleted()
    {
        PlayBuildingCaptureCompleteVoice();
    }

    private void HandleBuildingCaptureFailed()
    {
        PlayBuildingCaptureFailSfx();
    }

    private void HandleMedikitPickedUp()
    {
        PlayMedikitPickupVoice();
    }

    private void HandleUnitUpgraded()
    {
        PlayUnitUpgradeVoice();
    }

    private void HandleTechLevelUp()
    {
        PlayTechLevelUpVoice();
    }

    private void HandleInsufficientResources()
    {
        PlayInsufficientResourcesVoice();
    }

    private void HandleInvalidCommand()
    {
        PlayInvalidCommandVoice();
    }
}
