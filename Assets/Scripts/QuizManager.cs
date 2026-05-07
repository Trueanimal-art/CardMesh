using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuizManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private QuestionData questionData;
    [SerializeField] private bool shuffleQuestions = true;

    // ✅ НОВОЕ: сколько вопросов брать за одну попытку
    [SerializeField] private int questionsPerRun = 10;

    [Header("UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private AnswerButton[] answerButtons;

    [Header("Timing")]
    [SerializeField] private float nextQuestionDelay = 0.8f;

    // === META HOOKS ===
    public event Action<int, int> AnswerSelected; // (questionIndex, answerIndex)
    public event Action QuizCompleted;            // когда квиз закончился
    public int CurrentQuestionIndex { get; private set; } = -1;

    private List<int> _order = new List<int>();
    private int _currentOrderIndex = 0;
    private QuestionData.Question _currentQuestion;
    private bool _answered = false;

    private void Start()
    {
        if (questionData == null || questionData.questions == null || questionData.questions.Count == 0)
        {
            Debug.LogError("QuizManager: QuestionData не назначен или пустой.");
            return;
        }

        if (questionText == null)
        {
            Debug.LogError("QuizManager: questionText не назначен (TMP).");
            return;
        }

        if (answerButtons == null || answerButtons.Length == 0)
        {
            Debug.LogError("QuizManager: answerButtons не назначены.");
            return;
        }

        BuildOrder();
        ShowCurrentQuestion();
    }

    private void BuildOrder()
    {
        _order.Clear();

        // Собираем все индексы вопросов
        for (int i = 0; i < questionData.questions.Count; i++)
            _order.Add(i);

        // Перемешиваем, если нужно
        if (shuffleQuestions)
            Shuffle(_order);

        // ✅ ОГРАНИЧЕНИЕ НА КОЛИЧЕСТВО ВОПРОСОВ
        int total = questionData.questions.Count;
        int take = Mathf.Clamp(questionsPerRun, 1, total);

        if (_order.Count > take)
            _order.RemoveRange(take, _order.Count - take);

        _currentOrderIndex = 0;
    }

    private void ShowCurrentQuestion()
    {
        _answered = false;

        int qIndex = _order[_currentOrderIndex];
        CurrentQuestionIndex = qIndex;
        _currentQuestion = questionData.questions[qIndex];

        questionText.text = _currentQuestion.questionText;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            string ans = (i < _currentQuestion.answers.Length)
                ? _currentQuestion.answers[i]
                : "";

            answerButtons[i].Setup(ans, i, OnAnswerSelected);
            answerButtons[i].SetStateDefault();
        }
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        if (_answered) return;
        _answered = true;

        // === META: сообщаем, какой вопрос и какой ответ выбрали ===
        AnswerSelected?.Invoke(CurrentQuestionIndex, selectedIndex);

        // Блокируем кнопки
        for (int i = 0; i < answerButtons.Length; i++)
            answerButtons[i].SetStateDisabled();

        // Подсвечиваем выбранную
        if (selectedIndex >= 0 && selectedIndex < answerButtons.Length)
            answerButtons[selectedIndex].SetStateSelected();

        StartCoroutine(NextQuestionCoroutine());
    }

    private IEnumerator NextQuestionCoroutine()
    {
        yield return new WaitForSeconds(nextQuestionDelay);

        _currentOrderIndex++;

        if (_currentOrderIndex >= _order.Count)
        {
            // Конец квиза
            questionText.text = "Quiz complete!";

            for (int i = 0; i < answerButtons.Length; i++)
                answerButtons[i].SetStateDisabled();

            // === META: сигнал завершения ===
            QuizCompleted?.Invoke();
            yield break;
        }

        ShowCurrentQuestion();
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
