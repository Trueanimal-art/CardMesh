using UnityEngine;

public class MetaTracker : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private MetaWeightsData weightsData;
    [SerializeField] private MetaScore score;

    [Header("UI Panels")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Result Presenter")]
    [SerializeField] private ResultPanelController resultController;

    [Header("Ads")]
    [SerializeField] private InterstitialCounter interstitialCounter; // ✅ добавили

    [Header("Interstitial Settings (Main Quiz)")]
    [Tooltip("Сколько прохождений основного квиза БЕЗ рекламы. Например 2 => реклама на 3-й раз.")]
    [SerializeField] private int runsWithoutInterstitial = 2;

    [Tooltip("Ключ в PlayerPrefs для хранения счетчика прохождений основного квиза (между перезапусками сцен).")]
    [SerializeField] private string interstitialRunsPrefsKey = "MainQuiz_RunCount";

    [Tooltip("Включить показ interstitial из MetaTracker (рекомендуется).")]
    [SerializeField] private bool useMetaTrackerInterstitial = true;

    [Tooltip("Логи interstitial в консоль (можно выключить).")]
    [SerializeField] private bool interstitialDebugLogs = false;

    private void Awake()
    {
        if (score != null) score.ResetScore();
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (quizManager != null)
        {
            quizManager.AnswerSelected += OnAnswerSelected;
            quizManager.QuizCompleted += OnQuizCompleted;
        }
    }

    private void OnDisable()
    {
        if (quizManager != null)
        {
            quizManager.AnswerSelected -= OnAnswerSelected;
            quizManager.QuizCompleted -= OnQuizCompleted;
        }
    }

    private void OnAnswerSelected(int questionIndex, int answerIndex)
    {
        if (weightsData == null || score == null) return;

        var w = weightsData.GetWeights(questionIndex, answerIndex);
        score.Add(w);
    }

    private void OnQuizCompleted()
    {
        if (score == null) return;

        if (quizPanel != null) quizPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultController != null)
            resultController.ShowResult(score.GetWinner());

        // =========================
        // INTERSTITIAL: основной квиз (10 вопросов)
        // Логика: 2 прохода без рекламы, на 3-й показываем, потом снова.
        // Важно: счётчик хранится в PlayerPrefs, чтобы НЕ сбрасывался при перезагрузке сцен.
        // =========================
        if (useMetaTrackerInterstitial)
        {
            HandleMainQuizInterstitial();
        }
        else
        {
            // Старый вариант (если вдруг захочешь вернуть):
            if (interstitialCounter != null)
                interstitialCounter.OnQuizFinished_ShowInterstitialIfNeeded();
            else
                Debug.LogWarning("[MetaTracker] InterstitialCounter не назначен в инспекторе.");
        }
    }

    private void HandleMainQuizInterstitial()
    {
        int runCount = PlayerPrefs.GetInt(interstitialRunsPrefsKey, 0);
        runCount++;
        PlayerPrefs.SetInt(interstitialRunsPrefsKey, runCount);
        PlayerPrefs.Save();

        if (interstitialDebugLogs)
            Debug.Log($"[MetaTracker] MainQuiz finished. runCount={runCount}, runsWithoutInterstitial={runsWithoutInterstitial}");

        int showOn = runsWithoutInterstitial + 1;
        if (runCount < showOn)
            return;

        if (AdsManager.Instance == null)
        {
            Debug.LogWarning("[MetaTracker] AdsManager.Instance == null. Can't show interstitial.");
            return;
        }

        bool started = AdsManager.Instance.TryShowInterstitialWithResult(
            onClosedOrShown: () =>
            {
                if (interstitialDebugLogs)
                    Debug.Log("[MetaTracker] Interstitial closed. Resetting counter.");

                PlayerPrefs.SetInt(interstitialRunsPrefsKey, 0);
                PlayerPrefs.Save();
            },
            onFail: () =>
            {
                // Если показ НЕ стартанул — НЕ сбрасываем счётчик,
                // иначе получится "вечная реклама, которая никогда не показывается".
                if (interstitialDebugLogs)
                    Debug.LogWarning("[MetaTracker] Interstitial did not start. Keeping counter.");
            }
        );

        // started==false => onFail уже вызван, счетчик не трогаем
        if (interstitialDebugLogs)
            Debug.Log($"[MetaTracker] Interstitial started={started}");
    }
}
