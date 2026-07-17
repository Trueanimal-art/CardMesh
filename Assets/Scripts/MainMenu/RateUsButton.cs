using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class RateUsButton : MonoBehaviour
    {
        [SerializeField] private GameObject ratingPopup;

        public void OpenRatingPopup()
        {
            GameObject popup = ResolveRatingPopup();
            if (popup == null)
            {
                Debug.LogWarning("RateUsButton: rating popup is not assigned.");
                return;
            }

            popup.SetActive(true);
            popup.transform.SetAsLastSibling();
        }

        private GameObject ResolveRatingPopup()
        {
            GameObject bestPopup = ratingPopup;
            long bestOrder = GetHierarchyOrder(bestPopup != null ? bestPopup.transform : null);

            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate == null || candidate.name != "RateUsPopup" || !candidate.scene.IsValid())
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
    }
}
