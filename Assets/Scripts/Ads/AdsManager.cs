using System;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [SerializeField] private bool logDisabledState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (logDisabledState)
        {
            Debug.Log("AdsManager is disabled for the CardMesh MVP.");
        }
    }

    public void TryShowRewarded(Action onSuccess, Action onFailOrClosed)
    {
        if (logDisabledState)
        {
            Debug.Log("Rewarded ad skipped: ads are disabled for the CardMesh MVP.");
        }

        onFailOrClosed?.Invoke();
    }

    public void TryShowInterstitial(Action onClosedOrShown = null, Action onFail = null)
    {
        if (logDisabledState)
        {
            Debug.Log("Interstitial ad skipped: ads are disabled for the CardMesh MVP.");
        }

        onClosedOrShown?.Invoke();
    }

    public bool TryShowInterstitialWithResult(Action onClosedOrShown = null, Action onFail = null)
    {
        TryShowInterstitial(onClosedOrShown, onFail);
        return true;
    }

    public void ShowInterstitial()
    {
        TryShowInterstitial();
    }

    public void ShowRewarded(Action onSuccess)
    {
        TryShowRewarded(onSuccess, null);
    }
}
