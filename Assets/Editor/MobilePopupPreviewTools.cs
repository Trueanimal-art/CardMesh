#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CardMesh.Editor
{
    public static class MobilePopupPreviewTools
    {
        [MenuItem("Tools/CardMesh/Preview Daily Rewards")]
        public static void PreviewDailyRewards()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Daily Rewards preview: enter Play Mode first.");
                return;
            }

            DailyRewardsPopup popup =
                Object.FindFirstObjectByType<DailyRewardsPopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Daily Rewards preview: DailyRewardsPopup was not found.");
                return;
            }

            popup.Show(null);
        }

        [MenuItem("Tools/CardMesh/Preview Royal Series")]
        public static void PreviewRoyalSeries()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Royal Series preview: enter Play Mode first.");
                return;
            }

            ChestChasePopup popup =
                Object.FindFirstObjectByType<ChestChasePopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Royal Series preview: ChestChasePopup was not found.");
                return;
            }

            popup.Show(null);
        }

        [MenuItem("Tools/CardMesh/Preview Settings")]
        public static void PreviewSettings()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Settings preview: enter Play Mode first.");
                return;
            }

            SettingsPopup.GetOrCreate()?.Show();
        }

        [MenuItem("Tools/CardMesh/Preview Levels")]
        public static void PreviewLevels()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Levels preview: enter Play Mode first.");
                return;
            }

            LevelProgressionManager manager =
                Object.FindFirstObjectByType<LevelProgressionManager>(
                    FindObjectsInactive.Include);
            if (manager == null)
            {
                manager = new GameObject("DevLevelProgressionManager")
                    .AddComponent<LevelProgressionManager>();
            }

            LevelSelectionPopup.GetOrCreate()?.Show(manager, _ => { });
        }

        [MenuItem("Tools/CardMesh/Preview Contact")]
        public static void PreviewContact()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Contact preview: enter Play Mode first.");
                return;
            }

            ContactPopupController popup =
                Object.FindFirstObjectByType<ContactPopupController>(
                    FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Contact preview: ContactPopupController was not found.");
                return;
            }

            popup.Open();
        }

        [MenuItem("Tools/CardMesh/Preview Rate Us")]
        public static void PreviewRateUs()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Rate Us preview: enter Play Mode first.");
                return;
            }

            RateUsPopupController popup =
                Object.FindFirstObjectByType<RateUsPopupController>(
                    FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Rate Us preview: RateUsPopupController was not found.");
                return;
            }

            popup.Open();
        }
    }
}
#endif
