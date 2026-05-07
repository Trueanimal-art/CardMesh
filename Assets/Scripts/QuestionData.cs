using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestionData", menuName = "Quiz/Question Data")]
public class QuestionData : ScriptableObject
{
    [Serializable]
    public class Question
    {
        public string questionText;
        public string[] answers = new string[4];
        [Range(0, 3)]
        public int correctAnswerIndex;
    }

    public List<Question> questions = new List<Question>();
}
