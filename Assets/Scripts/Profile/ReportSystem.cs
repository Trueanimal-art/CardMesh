using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReportSystem : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private PlayerProfileSystem playerProfileSystem;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private RectTransform overlayParent;
    [SerializeField] private TMP_Text styleSourceText;

    [Header("Display")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -860f);
    [SerializeField] private Vector2 sizeDelta = new Vector2(860f, 150f);
    [SerializeField] private float fontSize = 22f;
    [SerializeField] private string reportTitle = "СИСТЕМНЫЙ ОТЧЁТ";

    private TextMeshProUGUI _overlayText;
    private Coroutine _showRoutine;

    private void Awake()
    {
        EnsureOverlay();
        HideOverlayImmediate();
    }

    private void OnEnable()
    {
        if (quizManager != null)
            quizManager.QuizCompleted += OnQuizCompleted;
    }

    private void OnDisable()
    {
        if (quizManager != null)
            quizManager.QuizCompleted -= OnQuizCompleted;

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        HideOverlayImmediate();
    }

    private void OnQuizCompleted()
    {
        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowReportWhenPanelIsReady());
    }

    private IEnumerator ShowReportWhenPanelIsReady()
    {
        float timeout = Time.unscaledTime + 2f;

        while (resultPanel != null && !resultPanel.activeInHierarchy)
        {
            if (Time.unscaledTime >= timeout)
                yield break;

            yield return null;
        }

        yield return null;

        ShowOverlay(BuildReport());
        _showRoutine = null;
    }

    private string BuildReport()
    {
        if (playerProfileSystem == null)
            return reportTitle;

        List<string> comments = new List<string>(3);
        var dominantAxis = playerProfileSystem.GetDominantAxis();
        comments.Add(BuildDominantComment(dominantAxis, playerProfileSystem.GetValue(dominantAxis)));

        AppendSecondaryComments(comments, dominantAxis);

        StringBuilder builder = new StringBuilder();
        builder.Append("<b>");
        builder.Append(reportTitle);
        builder.Append("</b>\n");

        int addedComments = 0;
        for (int i = 0; i < comments.Count && addedComments < 2; i++)
        {
            if (string.IsNullOrEmpty(comments[i]))
                continue;

            builder.Append(comments[i]);
            builder.Append('\n');
            addedComments++;
        }

        builder.Append("Рекомендация: ");
        builder.Append(GetRecommendation());

        return builder.ToString().TrimEnd();
    }

    private void AppendSecondaryComments(List<string> comments, PlayerProfileSystem.ProfileAxis dominantAxis)
    {
        PlayerProfileSystem.ProfileAxis[] order =
        {
            PlayerProfileSystem.ProfileAxis.Stress,
            PlayerProfileSystem.ProfileAxis.Passivity,
            PlayerProfileSystem.ProfileAxis.Aggression
        };

        for (int i = 0; i < order.Length && comments.Count < 3; i++)
        {
            if (order[i] == dominantAxis)
                continue;

            comments.Add(BuildAxisComment(order[i]));
        }
    }

    private string BuildAxisComment(PlayerProfileSystem.ProfileAxis axis)
    {
        switch (axis)
        {
            case PlayerProfileSystem.ProfileAxis.Aggression:
                return BuildAggressionComment(playerProfileSystem.Aggression);
            case PlayerProfileSystem.ProfileAxis.Stress:
                return BuildStressComment(playerProfileSystem.Stress);
            case PlayerProfileSystem.ProfileAxis.Passivity:
                return BuildPassivityComment(playerProfileSystem.Passivity);
            default:
                return string.Empty;
        }
    }

    private string BuildDominantComment(PlayerProfileSystem.ProfileAxis axis, int value)
    {
        switch (axis)
        {
            case PlayerProfileSystem.ProfileAxis.Aggression:
                return value >= 6
                    ? "Доминирует директивный контур. Давление выше нормы."
                    : "Доминирует жёсткий контур. Контроль становится приоритетом.";

            case PlayerProfileSystem.ProfileAxis.Stress:
                return value >= 6
                    ? "Доминирует напряжённый контур. Устойчивость решений снижена."
                    : "Доминирует тревожный фон. Видна внутренняя перегрузка.";

            case PlayerProfileSystem.ProfileAxis.Passivity:
                return value >= 6
                    ? "Доминирует выжидательный контур. Инициатива ниже нормы."
                    : "Доминирует пассивный режим. Решения чаще откладываются.";

            default:
                return "Система фиксирует нейтральный профиль без явного доминирования.";
        }
    }

    private static string BuildStressComment(int value)
    {
        if (value >= 6)
            return "Стресс высокий. Нужен контроль качества решений.";

        if (value >= 3)
            return "Стресс умеренный. Ресурс на восстановление расходуется.";

        return "Стресс находится в рабочем диапазоне.";
    }

    private static string BuildPassivityComment(int value)
    {
        if (value >= 6)
            return "Пассивный контур высокий. Скорость включения снижена.";

        if (value >= 3)
            return "Пассивный контур заметен. Решения идут с паузой.";

        return "Пассивный контур слабый. Инициатива сохранена.";
    }

    private static string BuildAggressionComment(int value)
    {
        if (value >= 6)
            return "Агрессивный контур высокий. Вероятно конфликтное давление.";

        if (value >= 3)
            return "Агрессивный контур умеренный. Директивность растёт.";

        return "Агрессивный контур остаётся фоновым.";
    }

    private string GetRecommendation()
    {
        if (playerProfileSystem == null)
            return "наблюдать.";

        var dominantAxis = playerProfileSystem.GetDominantAxis();
        int dominantValue = playerProfileSystem.GetValue(dominantAxis);
        int secondValue = playerProfileSystem.GetSecondHighestValue();

        if (dominantAxis == PlayerProfileSystem.ProfileAxis.Aggression && dominantValue >= 7 && dominantValue >= secondValue + 2)
            return "уволить.";

        if (dominantValue >= 6 || playerProfileSystem.Stress >= 6 || playerProfileSystem.Passivity >= 6)
            return "наблюдать.";

        return "повысить.";
    }

    private void EnsureOverlay()
    {
        if (_overlayText != null)
            return;

        RectTransform parent = overlayParent != null ? overlayParent : transform as RectTransform;
        if (parent == null)
            return;

        var overlayObject = new GameObject("ReportRuntimeText", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement), typeof(TextMeshProUGUI));
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

    private void ShowOverlay(string text)
    {
        if (_overlayText == null)
            return;

        _overlayText.text = text;
        _overlayText.gameObject.SetActive(true);
    }

    private void HideOverlayImmediate()
    {
        if (_overlayText == null)
            return;

        _overlayText.text = string.Empty;
        _overlayText.gameObject.SetActive(false);
    }
}
