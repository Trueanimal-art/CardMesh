using UnityEngine;

namespace CardMesh
{
    /// <summary>Keeps the main-menu canvas in a single-popup state.</summary>
    public static class MainMenuPopupExclusivity
    {
        public static void OpenExclusive(Component target)
        {
            if (target == null)
            {
                return;
            }

            GameObject targetRoot = target.gameObject;
            DeactivateOthers<DailyRewardsPopup>(targetRoot);
            DeactivateOthers<ChestChasePopup>(targetRoot);
            DeactivateOthers<CollectionBookPopup>(targetRoot);
            DeactivateOthers<ShopPopup>(targetRoot);
            DeactivateOthers<LevelSelectionPopup>(targetRoot);
            DeactivateOthers<SettingsPopup>(targetRoot);
            DeactivateOthers<ContactPopupController>(targetRoot);
            DeactivateOthers<RateUsPopupController>(targetRoot);
        }

        private static void DeactivateOthers<T>(GameObject targetRoot) where T : Component
        {
            T[] popups = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < popups.Length; i++)
            {
                T popup = popups[i];
                if (popup != null && popup.gameObject != targetRoot && popup.gameObject.activeSelf)
                {
                    popup.gameObject.SetActive(false);
                }
            }
        }
    }
}
