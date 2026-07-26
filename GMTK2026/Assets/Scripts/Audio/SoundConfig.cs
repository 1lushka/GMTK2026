using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundConfig", menuName = "Audio/Sound Config")]
public sealed class SoundConfig : ScriptableObject
{
    [Serializable]
    public sealed class SoundEntry
    {
        [SerializeField] private SoundId id;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] private bool spatial3D;
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField, Min(1)] private int maxVoices = 4;

        public SoundId Id => id;
        public float Volume => volume;
        public bool Spatial3D => spatial3D;
        public float Cooldown => cooldown;
        public int MaxVoices => Mathf.Max(1, maxVoices);
        public float RandomPitch => UnityEngine.Random.Range(
            Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));

        public AudioClip RandomClip
        {
            get
            {
                if (clips == null || clips.Length == 0) return null;
                int start = UnityEngine.Random.Range(0, clips.Length);
                for (int i = 0; i < clips.Length; i++)
                {
                    AudioClip clip = clips[(start + i) % clips.Length];
                    if (clip != null) return clip;
                }
                return null;
            }
        }
    }

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;

    [Header("Sound events")]
    [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();

    public AudioClip MenuMusic => menuMusic;
    public AudioClip GameMusic => gameMusic;
    public float MusicVolume => musicVolume;
    public IReadOnlyList<SoundEntry> Sounds => sounds;
}
