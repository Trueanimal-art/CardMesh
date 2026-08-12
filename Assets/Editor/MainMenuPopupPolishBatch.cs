using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardMesh.Editor
{
    public static class MainMenuPopupPolishBatch
    {
        [MenuItem("Tools/CardMesh/Rebuild All Main Menu Popups")]
        public static void RebuildAll()
        {
            DailyRewardsPopup.RebuildMainMenuDailyRewardsPopup();
            global::CardMesh.EditorTools.RoyalSeriesSceneInstaller.RebuildRoyalSeriesPopup();
            CollectionBookUiPolishInstaller.PolishCollectionBookAssets();
            ShopUiPolishInstaller.PolishShopPopup();
            ValidateStandardGeometry();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }

        private static void ValidateStandardGeometry()
        {
            LevelFrameChrome[] popups = UnityEngine.Object.FindObjectsByType<LevelFrameChrome>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Vector2 expectedSize = LevelFrameChrome.GetMetrics(LevelFrameChrome.Preset.Tall).PanelSize;
            Vector2 expectedPosition = LevelFrameChrome.GetMetrics(LevelFrameChrome.Preset.Tall).PanelPosition;
            Vector2? expectedClosePosition = null;
            int standardizedCount = 0;

            foreach (LevelFrameChrome popup in popups)
            {
                if (!Approximately(popup.CurrentMetrics.PanelSize, expectedSize))
                {
                    continue;
                }

                standardizedCount++;
                RectTransform panel = popup.transform as RectTransform;
                RectTransform badge = panel != null
                    ? panel.Find("TopBadge") as RectTransform ?? panel.Find("HeaderIcon") as RectTransform
                    : null;
                RectTransform close = panel != null ? panel.Find("CloseButton") as RectTransform : null;

                if (panel == null
                    || !Approximately(panel.sizeDelta, expectedSize)
                    || !Approximately(panel.anchoredPosition, expectedPosition)
                    || badge == null
                    || !Mathf.Approximately(badge.anchoredPosition.x, 0f)
                    || !Mathf.Approximately(badge.anchoredPosition.y, -168f)
                    || close == null)
                {
                    throw new InvalidOperationException(
                        $"Popup geometry drifted from the shared grid: {popup.transform.parent?.name}/{popup.name}; " +
                        $"size={panel?.sizeDelta}, position={panel?.anchoredPosition}, " +
                        $"badge={(badge != null ? badge.anchoredPosition.ToString() : "missing")}, " +
                        $"close={(close != null ? close.anchoredPosition.ToString() : "missing")}.");
                }

                expectedClosePosition ??= close.anchoredPosition;
                if (!Approximately(close.anchoredPosition, expectedClosePosition.Value))
                {
                    throw new InvalidOperationException($"Close-button centre drifted from the shared grid: {popup.name}.");
                }
            }

            if (standardizedCount != 4)
            {
                throw new InvalidOperationException(
                    $"Expected four standardized main-menu popups, found {standardizedCount}.");
            }
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
        }
    }
}
