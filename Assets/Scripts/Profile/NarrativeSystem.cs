using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NarrativeSystem : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private PlayerProfileSystem playerProfileSystem;
    [SerializeField] private RectTransform overlayParent;
    [SerializeField] private TMP_Text styleSourceText;

    [Header("Display")]
    [SerializeField] private int questionsPerComment = 3;
    [SerializeField] private float minimumVisibleSeconds = 3f;
    [SerializeField] private float nextQuestionTimeout = 2.5f;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -35f);
    [SerializeField] private Vector2 sizeDelta = new Vector2(980f, 90f);
    [SerializeField] private float fontSize = 32f;

    private TextMeshProUGUI _overlayText;
    private Coroutine _showRoutine;
    private Coroutine _hideRoutine;
    private bool _quizCompleted;
    private int _commentSequence;

    private void Awake()
    {
        EnsureOverlay();
        HideOverlayImmediate();
    }

    private void OnEnable()
    {
        _quizCompleted = false;

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

        StopActiveCoroutines();
        HideOverlayImmediate();
    }

    private void OnAnswerSelected(int questionIndex, int answerIndex)
    {
        if (playerProfileSystem == null || questionsPerComment <= 0)
            return;

        if (playerProfileSystem.AnswersProcessed <= 0)
            return;

        if (playerProfileSystem.AnswersProcessed % questionsPerComment != 0)
            return;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowCommentAfterNextQuestion(questionIndex));
    }

    private void OnQuizCompleted()
    {
        _quizCompleted = true;

        StopActiveCoroutines();
        HideOverlayImmediate();
    }

    private IEnumerator ShowCommentAfterNextQuestion(int answeredQuestionIndex)
    {
        float startedAt = Time.unscaledTime;

        while (!_quizCompleted && quizManager != null && quizManager.CurrentQuestionIndex == answeredQuestionIndex)
        {
            if (Time.unscaledTime - startedAt >= nextQuestionTimeout)
                yield break;

            yield return null;
        }

        if (_quizCompleted || quizManager == null)
            yield break;

        ShowOverlay(BuildComment());

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        _hideRoutine = StartCoroutine(HideAfterDelay(minimumVisibleSeconds * 2f));
        _showRoutine = null;
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HideOverlayImmediate();
        _hideRoutine = null;
    }

    private string BuildComment()
    {
        _commentSequence++;

        if (playerProfileSystem == null)
            return "Система не получила данных для промежуточной оценки.";

        switch (playerProfileSystem.GetDominantAxis())
        {
            case PlayerProfileSystem.ProfileAxis.Aggression:
                return GetAggressionComment(_commentSequence);
            case PlayerProfileSystem.ProfileAxis.Stress:
                return GetStressComment(_commentSequence);
            case PlayerProfileSystem.ProfileAxis.Passivity:
                return GetPassivityComment(_commentSequence);
            default:
                return "Система фиксирует нейтральный поведенческий контур.";
        }
    }

    private static string GetAggressionComment(int index)
    {
        string[] comments =
        {
            "Система фиксирует рост директивного давления.\nКонтур принятия решений становится жёстче.",
            "Система отмечает доминирование силового сценария.\nТолерантность к возражениям снижается.",
            "Система подтверждает приоритет прямого контроля.\nКомандный режим усиливается."
        };

        return comments[(index - 1) % comments.Length];
    }

    private static string GetStressComment(int index)
    {
        string[] comments =
        {
            "Система фиксирует накопление внутреннего напряжения.\nСтабильность решений проседает.",
            "Система отмечает перегрузку реактивного контура.\nРиск импульсивных действий повышен.",
            "Система подтверждает стрессовое смещение.\nРабочий ресурс расходуется ускоренно."
        };

        return comments[(index - 1) % comments.Length];
    }

    private static string GetPassivityComment(int index)
    {
        string[] comments =
        {
            "Система фиксирует уход в выжидательный режим.\nИнициатива подавлена.",
            "Система отмечает усиление пассивного контура.\nРешения всё чаще откладываются.",
            "Система подтверждает стратегию уклонения.\nГотовность брать ответственность снижена."
        };

        return comments[(index - 1) % comments.Length];
    }

    private void EnsureOverlay()
    {
        if (_overlayText != null)
            return;

        RectTransform parent = overlayParent != null ? overlayParent : transform as RectTransform;
        if (parent == null)
            return;

        var overlayObject = new GameObject("NarrativeRuntimeText", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement), typeof(TextMeshProUGUI));
        var rectTransform = overlayObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        var canvasGroup = overlayObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.ignoreParentGroups = false;

        var layoutElement = overlayObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        _overlayText = overlayObject.GetComponent<TextMeshProUGUI>();
        _overlayText.raycastTarget = false;
        _overlayText.richText = true;
        _overlayText.enableWordWrapping = true;
        _overlayText.overflowMode = TextOverflowModes.Ellipsis;
        _overlayText.alignment = TextAlignmentOptions.Top;
        _overlayText.fontSize = fontSize;
        _overlayText.text = string.Empty;

        ApplyStyleFromSource();
    }

    private void ApplyStyleFromSource()
    {
        if (_overlayText == null)
            return;

        if (styleSourceText == null)
        {
            _overlayText.color = Color.white;
            return;
        }

        _overlayText.font = styleSourceText.font;
        _overlayText.fontSharedMaterial = styleSourceText.fontSharedMaterial;
        _overlayText.color = styleSourceText.color;
        _overlayText.fontStyle = styleSourceText.fontStyle;
        _overlayText.characterSpacing = styleSourceText.characterSpacing;
        _overlayText.wordSpacing = styleSourceText.wordSpacing;
        _overlayText.lineSpacing = styleSourceText.lineSpacing;
        _overlayText.alignment = TextAlignmentOptions.Top;
    }

    private void ShowOverlay(string message)
    {
        if (_overlayText == null)
            return;

        _overlayText.text = message;
        _overlayText.gameObject.SetActive(true);
    }

    private void HideOverlayImmediate()
    {
        if (_overlayText == null)
            return;

        _overlayText.text = string.Empty;
        _overlayText.gameObject.SetActive(false);
    }

    private void StopActiveCoroutines()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
    }
}
