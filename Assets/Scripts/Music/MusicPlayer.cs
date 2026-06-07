using UnityEngine;
using System.Collections.Generic;

public class MusicPlayer : MonoBehaviour
{
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string AudioMutedKey = "AudioMuted";
    private const float DefaultMusicVolume = 0.7f;
    private const float DefaultSfxVolume = 0.8f;

    public static MusicPlayer Instance;

    [Header("Background Music")]
    [SerializeField] private AudioClip[] musicTracks = System.Array.Empty<AudioClip>();
    [SerializeField, Range(0f, 1f)] private float musicVolume = DefaultMusicVolume;

    [Header("SFX")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = DefaultSfxVolume;
    [SerializeField] private AudioClip buttonClickClip = null;
    [SerializeField] private AudioClip cardPickClip = null;
    [SerializeField] private AudioClip cardDropClip = null;
    [SerializeField] private AudioClip cardCompleteClip = null;
    [SerializeField] private AudioClip winClip = null;
    [SerializeField] private AudioClip loseClip = null;

    private AudioSource _audio;
    private AudioSource _sfxAudio;
    private readonly List<int> _remainingTrackIndices = new();
    private int _lastTrackIndex = -1;
    private bool _muted;

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.Save();
            ApplyAudioSourceSettings();
        }
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
            ApplyAudioSourceSettings();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audio = GetComponent<AudioSource>();
        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
        }

        _sfxAudio = EnsureSfxAudioSource();
        LoadSavedSettings();
        ApplyAudioSourceSettings();
    }

    private void Start()
    {
        PlayNextTrackIfNeeded();
    }

    private void Update()
    {
        if (_audio == null || _audio.isPlaying)
        {
            return;
        }

        PlayNextTrackIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        if (_audio == null)
        {
            _audio = GetComponent<AudioSource>();
        }

        _sfxAudio = EnsureSfxAudioSource();
        ApplyAudioSourceSettings();
    }
#endif

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = value;
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        PlayerPrefs.SetInt(AudioMutedKey, _muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudioSourceSettings();
    }

    public bool IsMuted()
    {
        return _muted;
    }

    public void ToggleMute()
    {
        SetMuted(!_muted);
    }

    public void PlayButtonClick()
    {
        PlaySfx(buttonClickClip);
    }

    public void PlayCardPick()
    {
        PlaySfx(cardPickClip);
    }

    public void PlayCardDrop()
    {
        PlaySfx(cardDropClip);
    }

    public void PlayCardComplete()
    {
        PlaySfx(cardCompleteClip);
    }

    public void PlayWin()
    {
        PlaySfx(winClip);
    }

    public void PlayLose()
    {
        PlaySfx(loseClip);
    }

    private void PlayNextTrackIfNeeded()
    {
        if (_audio == null)
        {
            return;
        }

        AudioClip nextClip = GetNextTrack();
        if (nextClip == null)
        {
            _audio.Stop();
            _audio.clip = null;
            return;
        }

        _audio.clip = nextClip;
        _audio.Play();
    }

    private AudioClip GetNextTrack()
    {
        List<int> validTrackIndices = GetValidTrackIndices();
        if (validTrackIndices.Count == 0)
        {
            return null;
        }

        if (validTrackIndices.Count == 1)
        {
            _lastTrackIndex = validTrackIndices[0];
            return musicTracks[_lastTrackIndex];
        }

        RemoveInvalidRemainingIndices(validTrackIndices);
        if (_remainingTrackIndices.Count == 0)
        {
            RefillRemainingTrackIndices(validTrackIndices);
        }

        int bagPosition = Random.Range(0, _remainingTrackIndices.Count);
        int trackIndex = _remainingTrackIndices[bagPosition];
        _remainingTrackIndices.RemoveAt(bagPosition);
        _lastTrackIndex = trackIndex;
        return musicTracks[trackIndex];
    }

    private List<int> GetValidTrackIndices()
    {
        List<int> validTrackIndices = new();
        if (musicTracks == null)
        {
            return validTrackIndices;
        }

        for (int index = 0; index < musicTracks.Length; index++)
        {
            if (musicTracks[index] != null)
            {
                validTrackIndices.Add(index);
            }
        }

        return validTrackIndices;
    }

    private void RefillRemainingTrackIndices(List<int> validTrackIndices)
    {
        _remainingTrackIndices.Clear();
        foreach (int index in validTrackIndices)
        {
            if (index != _lastTrackIndex)
            {
                _remainingTrackIndices.Add(index);
            }
        }

        if (_remainingTrackIndices.Count == 0)
        {
            _remainingTrackIndices.AddRange(validTrackIndices);
        }
    }

    private void RemoveInvalidRemainingIndices(List<int> validTrackIndices)
    {
        for (int index = _remainingTrackIndices.Count - 1; index >= 0; index--)
        {
            if (!validTrackIndices.Contains(_remainingTrackIndices[index]))
            {
                _remainingTrackIndices.RemoveAt(index);
            }
        }
    }

    private void ApplyAudioSourceSettings()
    {
        if (_audio == null)
        {
            return;
        }

        _audio.playOnAwake = false;
        _audio.loop = false;
        _audio.volume = musicVolume;
        _audio.mute = _muted;
        _audio.spatialBlend = 0f;

        if (_sfxAudio != null)
        {
            _sfxAudio.playOnAwake = false;
            _sfxAudio.loop = false;
            _sfxAudio.volume = sfxVolume;
            _sfxAudio.mute = _muted;
            _sfxAudio.spatialBlend = 0f;
        }
    }

    private void LoadSavedSettings()
    {
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
        _muted = PlayerPrefs.GetInt(AudioMutedKey, 0) == 1;
    }

    private AudioSource EnsureSfxAudioSource()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length > 1)
        {
            return sources[1];
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (_sfxAudio == null)
        {
            _sfxAudio = EnsureSfxAudioSource();
            ApplyAudioSourceSettings();
        }

        if (clip == null || _muted || _sfxAudio == null)
        {
            return;
        }

        _sfxAudio.PlayOneShot(clip, sfxVolume);
    }
}
