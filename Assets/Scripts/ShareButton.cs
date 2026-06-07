using TMPro;
using UnityEngine;

public class ShareButton : MonoBehaviour
{
    [Header("Source (Result UI)")]
    [SerializeField] private TMP_Text psychotypeTitleText;   // Например: "ОФИСНЫЙ ТРУП"
    [SerializeField] private TMP_Text psychotypeDescText;    // Описание с абзацами

    [Header("Game Info")]
    [SerializeField] private string gameName = "Офисный психопат";
    [SerializeField] private string link = ""; // Позже можно вставить ссылку на стор

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText; // Текст "Скопировано!"
    [SerializeField] private float feedbackSeconds = 1.2f;

    public void Share()
    {
        string type = psychotypeTitleText ? psychotypeTitleText.text.Trim() : "";
        string desc = psychotypeDescText ? psychotypeDescText.text.Trim() : "";

        string text =
            $"Я прошёл игру «{gameName}» и я: {type} 😈\n\n" +
            $"{desc}\n\n" +
            "А ты кто?";

        if (!string.IsNullOrEmpty(link))
            text += $"\n\n{link}";

        CopyToClipboard(text);
        ShowFeedback("Скопировано!");
    }

    private void CopyToClipboard(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLCopyToClipboard(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
    }

    private void ShowFeedback(string msg)
    {
        if (!feedbackText) return;

        feedbackText.text = msg;
        CancelInvoke(nameof(ClearFeedback));
        Invoke(nameof(ClearFeedback), feedbackSeconds);
    }

    private void ClearFeedback()
    {
        if (feedbackText)
            feedbackText.text = "";
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void WebGLCopyToClipboard(string text);
#endif
}
