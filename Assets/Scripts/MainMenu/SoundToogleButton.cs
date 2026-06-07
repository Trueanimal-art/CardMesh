using UnityEngine;
using UnityEngine.UI;

public class SoundToggleButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle toggle;          // дочерний Toggle
    [SerializeField] private GameObject checkmark;   // Toggle/Background/Checkmark (полоска)

    private bool _isApplying;

    private void Reset()
    {
        toggle = GetComponentInChildren<Toggle>(true);
    }

    private void Awake()
    {
        if (!toggle) toggle = GetComponentInChildren<Toggle>(true);
        if (!toggle) return;

        toggle.onValueChanged.RemoveListener(OnToggleChanged);
        toggle.onValueChanged.AddListener(OnToggleChanged);

        // Синхронизируем UI с реальным состоянием музыки
        SyncFromMusicPlayer();
    }

    private void OnEnable()
    {
        SyncFromMusicPlayer();
    }

    private void Start()
    {
        SyncFromMusicPlayer();
    }

    private void OnDestroy()
    {
        if (toggle) toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void SyncFromMusicPlayer()
    {
        // По умолчанию музыка играет => muted = false
        bool muted = false;

        if (MusicPlayer.Instance != null)
            muted = MusicPlayer.Instance.IsMuted();

        ApplyState(muted, updateToggle: true);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (_isApplying) return;

        // Договоримся так:
        // Toggle.isOn == true  => перечёркнуто => muted = true
        // Toggle.isOn == false => просто динамик => muted = false
        bool muted = isOn;

        if (MusicPlayer.Instance != null)
        {
            if (MusicPlayer.Instance.IsMuted() != muted)
                MusicPlayer.Instance.ToggleMute();
        }

        ApplyState(muted, updateToggle: false);
    }

    private void ApplyState(bool muted, bool updateToggle)
    {
        _isApplying = true;

        if (checkmark) checkmark.SetActive(muted);

        if (updateToggle && toggle)
            toggle.isOn = muted;

        _isApplying = false;
    }
}
