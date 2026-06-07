using UnityEngine;

public class InterstitialCounter : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Сколько прохождений БЕЗ interstitial. Например 2 => реклама на 3-й раз.")]
    [SerializeField] private int _runsWithoutInterstitial = 2;

    [Tooltip("Ключ в PlayerPrefs для хранения счетчика прохождений.")]
    [SerializeField] private string _prefsKey = "MainQuiz_RunCount";

    [Tooltip("Логи в консоль (можно выключить).")]
    [SerializeField] private bool _debugLogs = false;

    private bool _showInProgress = false;

    /// <summary>
    /// Вызывай этот метод в момент, когда основной квиз (10 вопросов) ЗАВЕРШЕН и показан результат.
    /// Важно: дергать именно на завершении квиза, а не на старте сцены.
    /// </summary>
    public void OnMainQuizFinished_ShowInterstitialIfNeeded()
    {
        if (_showInProgress)
        {
            if (_debugLogs) Debug.Log("[InterstitialCounter] Show already in progress, skipping.");
            return;
        }

        int runCount = PlayerPrefs.GetInt(_prefsKey, 0);
        runCount++;
        PlayerPrefs.SetInt(_prefsKey, runCount);
        PlayerPrefs.Save();

        if (_debugLogs) Debug.Log($"[InterstitialCounter] MainQuiz finished. runCount={runCount}, runsWithoutInterstitial={_runsWithoutInterstitial}");

        // Например: _runsWithoutInterstitial = 2
        // 1,2 => без рекламы
        // 3 => реклама
        int showOn = _runsWithoutInterstitial + 1;
        if (runCount < showOn)
            return;

        // Пора показывать interstitial
        EnsureBridge();

        _showInProgress = true;

        bool started = YandexAdsBridge.Instance.ShowInterstitial(() =>
        {
            // Сбрасываем счетчик ТОЛЬКО после закрытия рекламы
            if (_debugLogs) Debug.Log("[InterstitialCounter] Interstitial closed. Resetting counter.");
            PlayerPrefs.SetInt(_prefsKey, 0);
            PlayerPrefs.Save();

            _showInProgress = false;
        });

        if (!started)
        {
            // Показ не стартовал => НЕЛЬЗЯ сбрасывать счетчик, иначе он будет “закрывать рекламу” без показа вечно.
            if (_debugLogs) Debug.LogWarning("[InterstitialCounter] Interstitial did not start. Keeping counter, showInProgress=false.");
            _showInProgress = false;
        }
        else
        {
            if (_debugLogs) Debug.Log("[InterstitialCounter] Interstitial started.");
        }
    }

    // Для совместимости с твоими прошлыми вызовами (если где-то уже зовётся такое имя)
    public void OnQuizFinished_ShowInterstitialIfNeeded() => OnMainQuizFinished_ShowInterstitialIfNeeded();

    private void EnsureBridge()
    {
        if (YandexAdsBridge.Instance != null) return;

        if (_debugLogs) Debug.LogWarning("[InterstitialCounter] YandexAdsBridge.Instance == null. Creating runtime instance.");
        var go = new GameObject("YandexAdsBridge");
        go.AddComponent<YandexAdsBridge>();
    }

    // Удобно тестировать в редакторе через контекстное меню
    [ContextMenu("TEST: Simulate MainQuiz Finished")]
    private void TestSimulate()
    {
        OnMainQuizFinished_ShowInterstitialIfNeeded();
    }
}
