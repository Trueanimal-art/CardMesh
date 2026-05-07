using System;
using UnityEngine;

public class PlayerProfileSystem : MonoBehaviour
{
    [Serializable]
    private struct AnswerProfileEffect
    {
        public int aggression;
        public int stress;
        public int passivity;

        public AnswerProfileEffect(int aggressionValue, int stressValue, int passivityValue)
        {
            aggression = aggressionValue;
            stress = stressValue;
            passivity = passivityValue;
        }
    }

    public enum ProfileAxis
    {
        None,
        Aggression,
        Stress,
        Passivity
    }

    [Header("Links")]
    [SerializeField] private QuizManager quizManager;

    [Header("Hidden Profile Weights")]
    [SerializeField] private AnswerProfileEffect[] answerEffects = new AnswerProfileEffect[4]
    {
        new AnswerProfileEffect(2, 1, 0),
        new AnswerProfileEffect(0, 2, 1),
        new AnswerProfileEffect(1, 0, 2),
        new AnswerProfileEffect(1, 1, 1)
    };

    public int Aggression { get; private set; }
    public int Stress { get; private set; }
    public int Passivity { get; private set; }
    public int AnswersProcessed { get; private set; }

    public event Action ProfileChanged;

    private void Awake()
    {
        ResetProfile();
        EnsureEffectsArray();
    }

    private void OnValidate()
    {
        EnsureEffectsArray();
    }

    private void OnEnable()
    {
        if (quizManager != null)
            quizManager.AnswerSelected += OnAnswerSelected;
    }

    private void OnDisable()
    {
        if (quizManager != null)
            quizManager.AnswerSelected -= OnAnswerSelected;
    }

    public void ResetProfile()
    {
        Aggression = 0;
        Stress = 0;
        Passivity = 0;
        AnswersProcessed = 0;
    }

    public ProfileAxis GetDominantAxis()
    {
        if (Aggression <= 0 && Stress <= 0 && Passivity <= 0)
            return ProfileAxis.None;

        if (Aggression >= Stress && Aggression >= Passivity)
            return ProfileAxis.Aggression;

        if (Stress >= Aggression && Stress >= Passivity)
            return ProfileAxis.Stress;

        return ProfileAxis.Passivity;
    }

    public int GetValue(ProfileAxis axis)
    {
        switch (axis)
        {
            case ProfileAxis.Aggression:
                return Aggression;
            case ProfileAxis.Stress:
                return Stress;
            case ProfileAxis.Passivity:
                return Passivity;
            default:
                return 0;
        }
    }

    public int GetSecondHighestValue()
    {
        int first = Mathf.Max(Aggression, Mathf.Max(Stress, Passivity));

        if (first == Aggression)
            return Mathf.Max(Stress, Passivity);

        if (first == Stress)
            return Mathf.Max(Aggression, Passivity);

        return Mathf.Max(Aggression, Stress);
    }

    private void OnAnswerSelected(int questionIndex, int answerIndex)
    {
        if (answerIndex < 0 || answerIndex >= answerEffects.Length)
            return;

        var effect = answerEffects[answerIndex];

        Aggression += Mathf.Max(0, effect.aggression);
        Stress += Mathf.Max(0, effect.stress);
        Passivity += Mathf.Max(0, effect.passivity);
        AnswersProcessed++;

        ProfileChanged?.Invoke();
    }

    private void EnsureEffectsArray()
    {
        if (answerEffects == null || answerEffects.Length != 4)
        {
            answerEffects = new AnswerProfileEffect[4]
            {
                new AnswerProfileEffect(2, 1, 0),
                new AnswerProfileEffect(0, 2, 1),
                new AnswerProfileEffect(1, 0, 2),
                new AnswerProfileEffect(1, 1, 1)
            };
        }
    }
}
