using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class RateUsButton : MonoBehaviour
    {
        [SerializeField] private GameObject ratingPopup;

        public void OpenRatingPopup()
        {
            if (ratingPopup == null)
            {
                Debug.LogWarning("RateUsButton: rating popup is not assigned.");
                return;
            }

            ratingPopup.SetActive(true);
            ratingPopup.transform.SetAsLastSibling();
        }
    }
}
