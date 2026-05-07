using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MetaWeightsData", menuName = "WorkForUncle/Meta Weights Data")]
public class MetaWeightsData : ScriptableObject
{
    [Serializable]
    public class AnswerWeights
    {
        [Range(0, 3)] public int tihiyGeroy;
        [Range(0, 3)] public int corporatePolitik;
        [Range(0, 3)] public int prosvetlenniy;
        [Range(0, 3)] public int officeTrup;

        [Range(0, 3)] public int corporateGhost;
        [Range(0, 3)] public int officePhilosopher;

        // ✅ секретные
        [Range(0, 3)] public int callOverlord;
        [Range(0, 3)] public int airSalesman;
        [Range(0, 3)] public int vacationNinja;
        [Range(0, 3)] public int urgentKing;
    }

    [Serializable]
    public class QuestionWeights
    {
        // 4 ответа -> 4 набора весов
        public AnswerWeights[] answers = new AnswerWeights[4]
        {
            new AnswerWeights(), new AnswerWeights(), new AnswerWeights(), new AnswerWeights()
        };
    }

    // Индекс в этом списке должен соответствовать индексу вопроса в QuestionData.questions
    public List<QuestionWeights> questions = new List<QuestionWeights>();

    public AnswerWeights GetWeights(int questionIndex, int answerIndex)
    {
        if (questions == null) return null;
        if (questionIndex < 0 || questionIndex >= questions.Count) return null;

        var q = questions[questionIndex];
        if (q == null || q.answers == null) return null;
        if (answerIndex < 0 || answerIndex >= q.answers.Length) return null;

        return q.answers[answerIndex];
    }
}
