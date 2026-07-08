using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class RateUsPopupController : MonoBehaviour
    {
        private const string RatingPrefsKey = "CardMesh.Player.Rating";

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Image[] starImages = new Image[5];
        [SerializeField] private Sprite filledStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private int selectedRating = 5;

        private void OnEnable()
        {
            popupRoot ??= gameObject;
            selectedRating = Mathf.Clamp(selectedRating, 1, 5);
            RefreshStars();
        }

        public void SelectOne() => SetRating(1);
        public void SelectTwo() => SetRating(2);
        public void SelectThree() => SetRating(3);
        public void SelectFour() => SetRating(4);
        public void SelectFive() => SetRating(5);

        public void Submit()
        {
            PlayerPrefs.SetInt(RatingPrefsKey, Mathf.Clamp(selectedRating, 1, 5));
            PlayerPrefs.Save();
            Close();
        }

        public void Close()
        {
            (popupRoot != null ? popupRoot : gameObject).SetActive(false);
        }

        private void SetRating(int rating)
        {
            selectedRating = Mathf.Clamp(rating, 1, 5);
            RefreshStars();
        }

        private void RefreshStars()
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                Image image = starImages[i];
                if (image == null)
                {
                    continue;
                }

                bool active = i < selectedRating;
                image.sprite = active ? filledStarSprite : emptyStarSprite;
                image.color = Color.white;
            }
        }
    }
}
