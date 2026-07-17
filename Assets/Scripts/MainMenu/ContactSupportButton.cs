using System;
using System.Text;
using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class ContactSupportButton : MonoBehaviour
    {
        [SerializeField] private ContactPopupController contactPopup;
        [SerializeField] private string encodedSupportEmail = "";
        [SerializeField] private string subject = "CardMesh support";
        [SerializeField] private string body = "Здравствуйте!\n\n";

        public void OpenContactMail()
        {
            ContactPopupController popup = ResolveContactPopup();
            if (popup != null)
            {
                popup.Open();
                return;
            }

            OpenMailComposer(body);
        }

        private ContactPopupController ResolveContactPopup()
        {
            ContactPopupController bestPopup = contactPopup != null
                ? contactPopup
                : FindFirstObjectByType<ContactPopupController>(FindObjectsInactive.Include);
            long bestOrder = GetHierarchyOrder(bestPopup != null ? bestPopup.transform : null);

            foreach (ContactPopupController candidate in Resources.FindObjectsOfTypeAll<ContactPopupController>())
            {
                if (candidate == null || candidate.name != "ContactPopup" || !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                long candidateOrder = GetHierarchyOrder(candidate.transform);
                if (bestPopup == null || candidateOrder >= bestOrder)
                {
                    bestPopup = candidate;
                    bestOrder = candidateOrder;
                }
            }

            return bestPopup;
        }

        private static long GetHierarchyOrder(Transform transform)
        {
            if (transform == null || !transform.gameObject.scene.IsValid())
            {
                return long.MinValue;
            }

            long order = 0;
            Transform current = transform;
            while (current != null)
            {
                order = (order * 10000L) + current.GetSiblingIndex() + 1;
                current = current.parent;
            }

            return order;
        }

        internal void OpenMailComposer(string messageBody)
        {
            string email = DecodeEmail(encodedSupportEmail);
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            string url = $"mailto:{Uri.EscapeDataString(email)}" +
                         $"?subject={Uri.EscapeDataString(subject)}" +
                         $"&body={Uri.EscapeDataString(messageBody)}";
            Application.OpenURL(url);
        }

        internal static string DecodeEmail(string encoded)
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
