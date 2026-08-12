using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InterstitialOnRetryButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;

    [Header("Cooldown")]
    [Tooltip("0 = без ограничения")]
    [SerializeField] private float cooldownSeconds = 0f;

    [SerializeField] private string cooldownPrefsKey = "Interstitial_LastShownTime";

    [Header("After Ad Action")]
    [SerializeField] private UnityEvent onProceed;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private bool _clickInProgress;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // Автоподписка: чтобы Button.OnClick мог быть пустым
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    public void OnClick()
    {
        if (_clickInProgress) return;
        _clickInProgress = true;

        if (button != null) button.interactable = false;

        if (debugLogs) Debug.Log("[InterstitialOnRetryButton] Click");

        // cooldown
        if (cooldownSeconds > 0f)
        {
            double last = GetPrefsDouble(cooldownPrefsKey, 0);
            double now = Time.realtimeSinceStartupAsDouble;

            if ((now - last) < cooldownSeconds)
            {
                if (debugLogs) Debug.Log($"[InterstitialOnRetryButton] Cooldown active: {(now - last):0.00}s < {cooldownSeconds:0.00}s. Proceed.");
                Proceed();
                return;
            }
        }

        // Если Instance = null, попробуем найти AdsManager в сцене/DDD
        AdsManager adsManager = AdsManager.Instance;
        if (adsManager == null)
        {
            var found = FindFirstObjectByType<AdsManager>();
            if (debugLogs) Debug.Log($"[InterstitialOnRetryButton] AdsManager.Instance == null. Found in scene: {(found != null)}");

            // Если вообще нет AdsManager — продолжаем (но это и есть причина “без рекламы”)
            if (found == null)
            {
                if (debugLogs) Debug.LogWarning("[InterstitialOnRetryButton] No AdsManager in scene/DDD. Proceed WITHOUT AD.");
                Proceed();
                return;
            }

            adsManager = found;
        }

        bool started = adsManager.ShowInterstitialIfAllowed(
            AdsManager.ReasonRestart,
            onCompleted: () =>
            {
                if (debugLogs) Debug.Log("[InterstitialOnRetryButton] Interstitial closed. Proceed.");

                if (cooldownSeconds > 0f)
                {
                    SetPrefsDouble(cooldownPrefsKey, Time.realtimeSinceStartupAsDouble);
                }

                Proceed();
            },
            onSkippedOrFailed: () =>
            {
                if (debugLogs) Debug.LogWarning("[InterstitialOnRetryButton] Interstitial skipped or failed. Proceed WITHOUT AD.");
                Proceed();
            }
        );

        if (debugLogs) Debug.Log($"[InterstitialOnRetryButton] ShowInterstitialIfAllowed -> started={started}");

        // ShowInterstitialIfAllowed always invokes either onCompleted or onSkippedOrFailed.
    }

    private void Proceed()
    {
        if (button != null) button.interactable = true;
        _clickInProgress = false;
        onProceed?.Invoke();
    }

    private static void SetPrefsDouble(string key, double value)
    {
        PlayerPrefs.SetString(key, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }

    private static double GetPrefsDouble(string key, double defaultValue)
    {
        string s = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(s)) return defaultValue;

        if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
            return v;

        return defaultValue;
    }
}
