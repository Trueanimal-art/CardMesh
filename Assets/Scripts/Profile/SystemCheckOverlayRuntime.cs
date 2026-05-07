using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemCheckOverlayRuntime : MonoBehaviour
{
    private static readonly FieldInfo QuestionsPerCommentField =
        typeof(NarrativeSystem).GetField("questionsPerComment", BindingFlags.Instance | BindingFlags.NonPublic);

    [Header("Links")]
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private NarrativeSystem narrativeSystem;
    [SerializeField] private RectTransform overlayParent;
    [SerializeField] private TMP_Text styleSourceText;

    [Header("Display")]
    [SerializeField] private int questionsPerCheck = 3;
    [SerializeField] private int narrativeQuestionsPerComment = 2;
    [SerializeField] private float nextQuestionTimeout = 2.5f;
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(0f, 70f);
    [SerializeField] private Vector2 panelSize = new Vector2(1280f, 340f);

    public int Assertiveness { get; private set; }
    public int Stress { get; private set; }
    public int Adaptability { get; private set; }

    private RectTransform _overlayRoot;
    private Coroutine _hideNarrativeRoutine;
    private Coroutine _restoreNarrativeFrequencyRoutine;
    private TextMeshProUGUI _messageText;
    private Coroutine _showRoutine;
    private bool _quizCompleted;
    private int _answersProcessed;
    private bool _maskNarrativeForCurrentPrompt;

    private const string PromptText = "\u0421\u0438\u0441\u0442\u0435\u043c\u0430 \u0441\u043e\u043c\u043d\u0435\u0432\u0430\u0435\u0442\u0441\u044f \u0432 \u0432\u0430\u0448\u0435\u043c \u043e\u0442\u0432\u0435\u0442\u0435. \u041f\u043e\u0434\u0442\u0432\u0435\u0440\u0434\u0438\u0442\u044c \u043f\u043e\u0437\u0438\u0446\u0438\u044e?";
    private const string ConfirmText = "\u041f\u043e\u0434\u0442\u0432\u0435\u0440\u0434\u0438\u0442\u044c";
    private const string SwitchText = "\u0421\u043c\u0435\u043d\u0438\u0442\u044c \u0442\u0430\u043a\u0442\u0438\u043a\u0443";

    private void Awake()
    {
        if (narrativeSystem == null)
            narrativeSystem = GetComponent<NarrativeSystem>();

        ForceNarrativeFrequency();
        EnsureOverlay();
        HideOverlayImmediate();
        ResetSignals();
    }

    private void OnEnable()
    {
        _quizCompleted = false;
        ForceNarrativeFrequency();

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

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        StopRestoreNarrativeFrequencyRoutine();
        StopNarrativeHideRoutine();
        HideOverlayImmediate();
    }

    private void OnAnswerSelected(int questionIndex, int answerIndex)
    {
        _answersProcessed++;

        if (questionsPerCheck <= 0)
            return;

        if (_answersProcessed % questionsPerCheck != 0)
            return;

        _maskNarrativeForCurrentPrompt = ShouldMaskNarrativeForCurrentPrompt();
        if (_maskNarrativeForCurrentPrompt)
            SuppressNarrativeForCurrentAnswer();

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowOverlayAfterNextQuestion(questionIndex));
    }

    private void OnQuizCompleted()
    {
        _quizCompleted = true;

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        StopRestoreNarrativeFrequencyRoutine();
        StopNarrativeHideRoutine();
        HideOverlayImmediate();
    }

    private IEnumerator ShowOverlayAfterNextQuestion(int answeredQuestionIndex)
    {
        float startedAt = Time.unscaledTime;

        while (!_quizCompleted && quizManager != null && quizManager.CurrentQuestionIndex == answeredQuestionIndex)
        {
            if (Time.unscaledTime - startedAt >= nextQuestionTimeout)
            {
                _showRoutine = null;
                yield break;
            }

            yield return null;
        }

        if (_quizCompleted || quizManager == null)
        {
            _showRoutine = null;
            yield break;
        }

        ShowOverlay();
        _showRoutine = null;
    }

    private void ResetSignals()
    {
        Assertiveness = 0;
        Stress = 0;
        Adaptability = 0;
    }

    private void ApplyConfirmChoice()
    {
        Assertiveness += 2;
        Stress += 1;
        HideOverlayImmediate();
    }

    private void ApplySwitchChoice()
    {
        Adaptability += 2;
        Stress += 1;
        HideOverlayImmediate();
    }

    private void ShowOverlay()
    {
        EnsureOverlay();
        if (_overlayRoot == null)
            return;

        _messageText.text = PromptText;
        _overlayRoot.gameObject.SetActive(true);
        _overlayRoot.SetAsLastSibling();

        if (_maskNarrativeForCurrentPrompt)
        {
            StopNarrativeHideRoutine();
            HideNarrativeOverlay();
            _hideNarrativeRoutine = StartCoroutine(HideNarrativeWhileVisible());
        }
    }

    private void HideOverlayImmediate()
    {
        if (_overlayRoot == null)
            return;

        StopNarrativeHideRoutine();
        _overlayRoot.gameObject.SetActive(false);
    }

    private void EnsureOverlay()
    {
        if (_overlayRoot != null)
            return;

        if (overlayParent == null)
        {
            Debug.LogWarning("SystemCheckOverlayRuntime: overlayParent is not assigned.");
            return;
        }

        var rootObject = new GameObject("SystemCheckOverlayRuntime", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
        _overlayRoot = rootObject.GetComponent<RectTransform>();
        _overlayRoot.SetParent(overlayParent, false);
        _overlayRoot.anchorMin = Vector2.zero;
        _overlayRoot.anchorMax = Vector2.one;
        _overlayRoot.offsetMin = Vector2.zero;
        _overlayRoot.offsetMax = Vector2.zero;

        var canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.ignoreParentGroups = false;

        var layoutElement = rootObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        CreateBlocker(_overlayRoot);
        CreatePanel(_overlayRoot);
    }

    private void CreateBlocker(RectTransform parent)
    {
        var blockerObject = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image));
        var blockerRect = blockerObject.GetComponent<RectTransform>();
        blockerRect.SetParent(parent, false);
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        var blockerImage = blockerObject.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.01f);
        blockerImage.raycastTarget = true;
    }

    private void CreatePanel(RectTransform parent)
    {
        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = panelAnchoredPosition;
        panelRect.sizeDelta = panelSize;

        var panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.11f, 0.08f, 0.12f, 0.95f);
        panelImage.raycastTarget = true;

        _messageText = CreateMessage(panelRect);
        CreateButton(panelRect, "ConfirmButton", ConfirmText, new Vector2(-300f, -92f), ApplyConfirmChoice, new Color(0.86f, 0.48f, 0.12f, 1f));
        CreateButton(panelRect, "SwitchButton", SwitchText, new Vector2(300f, -92f), ApplySwitchChoice, new Color(0.27f, 0.45f, 0.80f, 1f));
    }

    private TextMeshProUGUI CreateMessage(RectTransform parent)
    {
        var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(70f, 150f);
        textRect.offsetMax = new Vector2(-70f, -50f);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 44f;
        text.text = string.Empty;

        ApplyTextStyle(text, Color.white, 44f);
        return text;
    }

    private Button CreateButton(RectTransform parent, string objectName, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick, Color backgroundColor)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(460f, 96f);

        var image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = true;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(onClick);

        var colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.5f);
        button.colors = colors;

        CreateButtonLabel(buttonRect, label);
        return button;
    }

    private void CreateButtonLabel(RectTransform parent, string label)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(parent, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);

        var text = labelObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 34f;

        ApplyTextStyle(text, Color.white, 34f);
    }

    private void ApplyTextStyle(TextMeshProUGUI target, Color fallbackColor, float fallbackFontSize)
    {
        if (target == null)
            return;

        target.fontSize = fallbackFontSize;
        target.color = fallbackColor;

        if (styleSourceText == null)
            return;

        target.font = styleSourceText.font;
        target.fontSharedMaterial = styleSourceText.fontSharedMaterial;
        target.fontStyle = styleSourceText.fontStyle;
        target.characterSpacing = styleSourceText.characterSpacing;
        target.wordSpacing = styleSourceText.wordSpacing;
        target.lineSpacing = styleSourceText.lineSpacing;
    }

    private void ForceNarrativeFrequency()
    {
        if (narrativeSystem == null || QuestionsPerCommentField == null)
            return;

        QuestionsPerCommentField.SetValue(narrativeSystem, Mathf.Max(1, narrativeQuestionsPerComment));
    }

    private bool ShouldMaskNarrativeForCurrentPrompt()
    {
        int normalizedNarrativeInterval = Mathf.Max(1, narrativeQuestionsPerComment);
        int overlapInterval = LeastCommonMultiple(Mathf.Max(1, questionsPerCheck), normalizedNarrativeInterval);
        return _answersProcessed % overlapInterval == 0;
    }

    private static int LeastCommonMultiple(int a, int b)
    {
        return Mathf.Abs(a * b) / GreatestCommonDivisor(a, b);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            int remainder = a % b;
            a = b;
            b = remainder;
        }

        return Mathf.Abs(a);
    }

    private IEnumerator HideNarrativeWhileVisible()
    {
        while (_overlayRoot != null && _overlayRoot.gameObject.activeSelf)
        {
            HideNarrativeOverlay();
            yield return null;
        }

        _hideNarrativeRoutine = null;
    }

    private void StopNarrativeHideRoutine()
    {
        if (_hideNarrativeRoutine != null)
        {
            StopCoroutine(_hideNarrativeRoutine);
            _hideNarrativeRoutine = null;
        }
    }

    private void HideNarrativeOverlay()
    {
        if (overlayParent == null)
            return;

        Transform narrativeTransform = overlayParent.Find("NarrativeRuntimeText");
        if (narrativeTransform == null)
            return;

        GameObject narrativeObject = narrativeTransform.gameObject;
        if (narrativeObject.activeSelf)
            narrativeObject.SetActive(false);
    }

    private void SuppressNarrativeForCurrentAnswer()
    {
        if (narrativeSystem == null || QuestionsPerCommentField == null)
            return;

        QuestionsPerCommentField.SetValue(narrativeSystem, int.MaxValue);

        StopRestoreNarrativeFrequencyRoutine();
        _restoreNarrativeFrequencyRoutine = StartCoroutine(RestoreNarrativeFrequencyNextFrame());
    }

    private IEnumerator RestoreNarrativeFrequencyNextFrame()
    {
        yield return null;
        ForceNarrativeFrequency();
        _restoreNarrativeFrequencyRoutine = null;
    }

    private void StopRestoreNarrativeFrequencyRoutine()
    {
        if (_restoreNarrativeFrequencyRoutine != null)
        {
            StopCoroutine(_restoreNarrativeFrequencyRoutine);
            _restoreNarrativeFrequencyRoutine = null;
        }
    }
}
