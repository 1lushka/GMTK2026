using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SoundManager : MonoBehaviour
{
    private sealed class Voice
    {
        public AudioSource Source;
        public SoundId Id;
    }

    public static SoundManager Instance { get; private set; }

    [SerializeField] private SoundConfig config;
    [SerializeField, Min(1)] private int initialVoiceCount = 12;

    private readonly Dictionary<SoundId, SoundConfig.SoundEntry> entries = new();
    private readonly Dictionary<SoundId, float> lastPlayTimes = new();
    private readonly List<Voice> voices = new();
    private AudioSource musicSource;
    private bool musicMutedForPause;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLookup();
        CreateMusicSource();
        gameObject.AddComponent<GameplaySoundHooks>();
        for (int i = 0; i < initialVoiceCount; i++) CreateVoice();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        PlaySceneMusic(SceneManager.GetActiveScene());
        UISoundRelay.InstallForScene();
    }

    private void Update()
    {
        bool shouldMute = Time.timeScale <= 0f;
        if (shouldMute == musicMutedForPause || musicSource == null) return;
        musicMutedForPause = shouldMute;
        musicSource.mute = shouldMute;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static AudioSource Play(SoundId id)
    {
        return Instance != null ? Instance.PlayInternal(id, null, false) : null;
    }

    public static AudioSource PlayAt(SoundId id, Vector3 position)
    {
        return Instance != null ? Instance.PlayInternal(id, position, false) : null;
    }

    public static AudioSource PlayLoopAt(SoundId id, Vector3 position)
    {
        return Instance != null ? Instance.PlayInternal(id, position, true) : null;
    }

    public static void Stop(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.loop = false;
    }

    private AudioSource PlayInternal(SoundId id, Vector3? position, bool loop)
    {
        if (!entries.TryGetValue(id, out SoundConfig.SoundEntry entry)) return null;
        AudioClip clip = entry.RandomClip;
        if (clip == null || IsCoolingDown(id, entry.Cooldown) || CountPlaying(id) >= entry.MaxVoices)
            return null;

        Voice voice = GetFreeVoice();
        AudioSource source = voice.Source;
        voice.Id = id;
        source.transform.position = position ?? transform.position;
        source.clip = clip;
        source.volume = entry.Volume;
        source.pitch = entry.RandomPitch;
        source.spatialBlend = entry.Spatial3D ? 1f : 0f;
        source.loop = loop;
        source.Play();
        lastPlayTimes[id] = Time.unscaledTime;
        return source;
    }

    private bool IsCoolingDown(SoundId id, float cooldown)
    {
        return cooldown > 0f && lastPlayTimes.TryGetValue(id, out float lastTime) &&
               Time.unscaledTime - lastTime < cooldown;
    }

    private int CountPlaying(SoundId id)
    {
        int count = 0;
        foreach (Voice voice in voices)
            if (voice.Id == id && voice.Source.isPlaying) count++;
        return count;
    }

    private Voice GetFreeVoice()
    {
        foreach (Voice voice in voices)
            if (!voice.Source.isPlaying) return voice;
        return CreateVoice();
    }

    private Voice CreateVoice()
    {
        GameObject voiceObject = new GameObject($"SFX Voice {voices.Count + 1}");
        voiceObject.transform.SetParent(transform, false);
        AudioSource source = voiceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        Voice voice = new Voice { Source = source };
        voices.Add(voice);
        return voice;
    }

    private void BuildLookup()
    {
        entries.Clear();
        if (config == null) return;
        foreach (SoundConfig.SoundEntry entry in config.Sounds)
            if (entry != null) entries[entry.Id] = entry;
    }

    private void CreateMusicSource()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneMusic(scene);
        UISoundRelay.InstallForScene();
    }

    private void PlaySceneMusic(Scene scene)
    {
        if (config == null || musicSource == null) return;
        AudioClip nextClip = scene.name == "MenuScene" ? config.MenuMusic : config.GameMusic;
        if (musicSource.clip == nextClip && musicSource.isPlaying) return;

        musicSource.Stop();
        musicSource.clip = nextClip;
        musicSource.volume = config.MusicVolume;
        musicSource.mute = Time.timeScale <= 0f;
        if (nextClip != null) musicSource.Play();
    }
}
