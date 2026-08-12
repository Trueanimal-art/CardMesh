using UnityEngine;

namespace CardMesh
{
    /// <summary>
    /// Keeps the authored MainMenu geometry untouched while a modal popup is open.
    /// Only visibility changes at runtime; RectTransform values remain scene-authored.
    /// </summary>
    public static class MainMenuNavigationVisibility
    {
        private const string ChromeRootName = "MainMenuChromeRoot";
        private const string BottomNavigationName = "BottomNavigation";

        public static void SetBottomNavigationVisible(Component context, bool visible)
        {
            if (context != null && context.gameObject.scene.IsValid())
            {
                foreach (GameObject root in context.gameObject.scene.GetRootGameObjects())
                {
                    if (SetVisibleInScope(
                            root.GetComponentsInChildren<RectTransform>(true),
                            visible))
                    {
                        return;
                    }
                }
            }

            SetVisibleInScope(
                Object.FindObjectsByType<RectTransform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                visible);
        }

        private static bool SetVisibleInScope(
            RectTransform[] rects,
            bool visible)
        {
            if (SetNamedVisible(rects, ChromeRootName, visible))
            {
                return true;
            }

            // Legacy fallback for scenes not yet migrated to MainMenuChromeRoot.
            return SetNamedVisible(rects, BottomNavigationName, visible);
        }

        private static bool SetNamedVisible(
            RectTransform[] rects,
            string targetName,
            bool visible)
        {
            foreach (RectTransform rect in rects)
            {
                if (rect == null
                    || rect.name != targetName
                    || !rect.gameObject.scene.IsValid())
                {
                    continue;
                }

                rect.gameObject.SetActive(visible);
                return true;
            }

            return false;
        }
    }
}
