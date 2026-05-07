using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance;

    private AudioSource _audio;

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
    }

    public void SetMuted(bool muted)
    {
        if (_audio != null) _audio.mute = muted;
    }

    public bool IsMuted()
    {
        return _audio != null && _audio.mute;
    }
}
