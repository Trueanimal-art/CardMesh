using System;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;

public class YandexAdsBridge : MonoBehaviour
{
    public static YandexAdsBridge Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void YG_ShowInterstitial();
    [DllImport("__Internal")] private static extern void YG_ShowRewarded();
#endif

    private Action _rewardedSuccess;
    private Action _rewardedClosed;
    private Action _interstitialClosed;

    private bool _rewardedShowing;
    private bool _interstitialShowing;

    private Coroutine _rewardedTimeoutRoutine;
    private Coroutine _interstitialTimeoutRoutine;

    // Таймауты, чтобы НЕ залипать, если SDK не прислал коллбек
    private const float REWARDED_TIMEOUT_SEC = 35f;
    private const float INTERSTITIAL_TIMEOUT_SEC = 30f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Важно для SendMessage из .jslib
        gameObject.name = "YandexAdsBridge";
    }

    // ===== PUBLIC API =====

    public bool ShowInterstitial(Action onClosed)
    {
        if (_interstitialShowing)
        {
            // Уже показываем или залипли. НЕ дергаем onClosed, иначе логика думает "реклама закрылась".
            return false;
        }

        _interstitialClosed = onClosed;

#if UNITY_WEBGL && !UNITY_EDITOR
        _interstitialShowing = true;
        StartInterstitialTimeout();
        YG_ShowInterstitial();
        return true;
#else
        onClosed?.Invoke();
        return false;
#endif
    }

    public bool ShowRewarded(Action onSuccess, Action onClosed)
    {
        if (_rewardedShowing)
        {
            return false;
        }

        _rewardedSuccess = onSuccess;
        _rewardedClosed = onClosed;

#if UNITY_WEBGL && !UNITY_EDITOR
        _rewardedShowing = true;
        StartRewardedTimeout();
        YG_ShowRewarded();
        return true;
#else
        onSuccess?.Invoke();
        onClosed?.Invoke();
        return false;
#endif
    }

    // ===== CALLBACKS FROM JS (SendMessage) =====

    public void OnInterstitialClosed()
    {
        if (!_interstitialShowing) return;

        _interstitialShowing = false;
        StopInterstitialTimeout();

        _interstitialClosed?.Invoke();
        _interstitialClosed = null;
    }

    public void OnRewardedSuccess()
    {
        _rewardedSuccess?.Invoke();
    }

    public void OnRewardedClosed()
    {
        if (!_rewardedShowing) return;

        _rewardedShowing = false;
        StopRewardedTimeout();

        _rewardedClosed?.Invoke();

        _rewardedSuccess = null;
        _rewardedClosed = null;
    }

    // ===== TIMEOUTS =====

    private void StartRewardedTimeout()
    {
        StopRewardedTimeout();
        _rewardedTimeoutRoutine = StartCoroutine(RewardedTimeoutCoroutine());
    }

    private void StopRewardedTimeout()
    {
        if (_rewardedTimeoutRoutine != null)
        {
            StopCoroutine(_rewardedTimeoutRoutine);
            _rewardedTimeoutRoutine = null;
        }
    }

    private IEnumerator RewardedTimeoutCoroutine()
    {
        yield return new WaitForSecondsRealtime(REWARDED_TIMEOUT_SEC);

        if (!_rewardedShowing) yield break;

        Debug.LogWarning("[YandexAdsBridge] Rewarded TIMEOUT. Forcing close to avoid stuck state.");
        _rewardedShowing = false;

        // Закрытие без награды
        _rewardedClosed?.Invoke();

        _rewardedSuccess = null;
        _rewardedClosed = null;
        _rewardedTimeoutRoutine = null;
    }

    private void StartInterstitialTimeout()
    {
        StopInterstitialTimeout();
        _interstitialTimeoutRoutine = StartCoroutine(InterstitialTimeoutCoroutine());
    }

    private void StopInterstitialTimeout()
    {
        if (_interstitialTimeoutRoutine != null)
        {
            StopCoroutine(_interstitialTimeoutRoutine);
            _interstitialTimeoutRoutine = null;
        }
    }

    private IEnumerator InterstitialTimeoutCoroutine()
    {
        yield return new WaitForSecondsRealtime(INTERSTITIAL_TIMEOUT_SEC);

        if (!_interstitialShowing) yield break;

        Debug.LogWarning("[YandexAdsBridge] Interstitial TIMEOUT. Forcing close to avoid stuck state.");
        _interstitialShowing = false;

        _interstitialClosed?.Invoke();
        _interstitialClosed = null;

        _interstitialTimeoutRoutine = null;
    }
}
