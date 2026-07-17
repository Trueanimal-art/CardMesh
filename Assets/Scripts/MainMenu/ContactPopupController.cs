using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class ContactPopupController : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private string contactEndpointUrl = "";
        [SerializeField] private string encodedSupportEmail = "";
        [SerializeField] private string subject = "CardMesh support";
        [SerializeField] private int maxMessageLength = 1000;
        [SerializeField] private bool fallbackToMailClient = true;

        private Coroutine sendRoutine;

        private void Awake()
        {
            popupRoot ??= gameObject;
            if (messageInput != null)
            {
                messageInput.characterLimit = Mathf.Max(1, maxMessageLength);
            }
        }

        private void OnEnable()
        {
            popupRoot ??= gameObject;
            SetSending(false);
        }

        public void Open()
        {
            GameObject root = popupRoot != null ? popupRoot : gameObject;
            root.SetActive(true);
            root.transform.SetAsLastSibling();

            if (messageInput != null)
            {
                messageInput.text = string.Empty;
                messageInput.characterLimit = Mathf.Max(1, maxMessageLength);
                messageInput.ActivateInputField();
            }

            SetStatus("До 1000 символов");
            SetSending(false);
        }

        public void Submit()
        {
            if (sendRoutine != null)
            {
                return;
            }

            string message = GetMessage();
            if (string.IsNullOrWhiteSpace(message))
            {
                SetStatus("Введите сообщение");
                return;
            }

            if (string.IsNullOrWhiteSpace(contactEndpointUrl))
            {
                if (fallbackToMailClient && TryOpenMailFallback(message))
                {
                    Close();
                    return;
                }

                SetStatus("Отправка не настроена");
                return;
            }

            sendRoutine = StartCoroutine(SendContactMessage(message));
        }

        public void Close()
        {
            if (sendRoutine != null)
            {
                StopCoroutine(sendRoutine);
                sendRoutine = null;
            }

            SetSending(false);
            (popupRoot != null ? popupRoot : gameObject).SetActive(false);
        }

        private IEnumerator SendContactMessage(string message)
        {
            SetSending(true);
            SetStatus("Отправляем...");

            ContactPayload payload = new()
            {
                recipientEmail = ContactSupportButton.DecodeEmail(encodedSupportEmail),
                subject = subject,
                message = message,
                appVersion = Application.version,
                platform = Application.platform.ToString(),
                deviceModel = SystemInfo.deviceModel,
                systemLanguage = Application.systemLanguage.ToString(),
                sentAtUtc = DateTime.UtcNow.ToString("O")
            };

            string json = JsonUtility.ToJson(payload);
            using UnityWebRequest request = new(contactEndpointUrl.Trim(), UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success &&
                           request.responseCode >= 200 &&
                           request.responseCode < 300;
            if (success)
            {
                SetStatus("Сообщение отправлено");
                yield return new WaitForSecondsRealtime(0.45f);
                sendRoutine = null;
                Close();
                yield break;
            }

            Debug.LogWarning($"ContactPopupController: send failed ({request.responseCode}) {request.error}");
            SetStatus("Не удалось отправить");
            SetSending(false);
            sendRoutine = null;
        }

        private bool TryOpenMailFallback(string message)
        {
            string email = ContactSupportButton.DecodeEmail(encodedSupportEmail);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string body = $"Здравствуйте!\n\n{message}\n\n";
            string url = $"mailto:{Uri.EscapeDataString(email)}" +
                         $"?subject={Uri.EscapeDataString(subject)}" +
                         $"&body={Uri.EscapeDataString(body)}";
            Application.OpenURL(url);
            return true;
        }

        private string GetMessage()
        {
            string message = messageInput != null ? messageInput.text : string.Empty;
            message = (message ?? string.Empty).Trim();
            int limit = Mathf.Max(1, maxMessageLength);
            return message.Length <= limit ? message : message.Substring(0, limit);
        }

        private void SetSending(bool isSending)
        {
            if (submitButton != null)
            {
                submitButton.interactable = !isSending;
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = !isSending;
            }

            if (messageInput != null)
            {
                messageInput.interactable = !isSending;
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
            RuntimeCyrillicFontProvider.Apply(statusLabel, message);
        }

        [Serializable]
        private sealed class ContactPayload
        {
            public string recipientEmail;
            public string subject;
            public string message;
            public string appVersion;
            public string platform;
            public string deviceModel;
            public string systemLanguage;
            public string sentAtUtc;
        }
    }
}
