using System;
using System.Text;
using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class ContactSupportButton : MonoBehaviour
    {
        [SerializeField] private string encodedSupportEmail = "";
        [SerializeField] private string subject = "CardMesh support";
        [SerializeField] private string body = "Здравствуйте!\n\n";

        public void OpenContactMail()
        {
            string email = DecodeEmail(encodedSupportEmail);
            if (string.IsNullOrWhiteSpace(email))
            {
                Debug.LogWarning("ContactSupportButton: support email is not configured.");
                return;
            }

            string url = $"mailto:{Uri.EscapeDataString(email)}" +
                         $"?subject={Uri.EscapeDataString(subject)}" +
                         $"&body={Uri.EscapeDataString(body)}";
            Application.OpenURL(url);
        }

        private static string DecodeEmail(string encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return string.Empty;
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Trim();
            }
            catch (FormatException)
            {
                return encoded.Trim();
            }
        }
    }
}
