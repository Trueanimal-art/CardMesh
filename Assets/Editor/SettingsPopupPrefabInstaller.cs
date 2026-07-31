using System.IO;
using CardMesh;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SettingsPopupPrefabInstaller
{
    private const string PrefabPath = "Assets/Resources/UI/MainMenu/SettingsPopup.prefab";
    private const string MainMenuScenePath = "Assets/MainMenu.unity";

    [MenuItem("Tools/CardMesh/Rebuild Settings Popup Prefab")]
    public static void RebuildPrefabAndMainMenuInstance()
    {
        GameObject prefab = RebuildPrefab();
        InstallPrefabIntoMainMenu(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static GameObject RebuildPrefab()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        GameObject root = CreateAuthoringInstance();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        if (prefab == null)
        {
            throw new UnityException($"Failed to save settings popup prefab at {PrefabPath}.");
        }

        return prefab;
    }

    private static void InstallPrefabIntoMainMenu(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            throw new UnityException($"Cannot install {PrefabPath}: Canvas not found in {MainMenuScenePath}.");
        }

        GameObject instance = null;
        foreach (SettingsPopup existing in Object.FindObjectsByType<SettingsPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject);
            if (source == prefab && instance == null)
            {
                instance = existing.gameObject;
            }
            else
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        if (instance == null)
        {
            instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        }

        if (instance == null)
        {
            throw new UnityException($"Cannot instantiate {PrefabPath} into {MainMenuScenePath}.");
        }

        instance.name = "SettingsPopup";
        instance.transform.SetParent(canvas.transform, false);
        ApplyStretch(instance.transform as RectTransform);
        SetLayerRecursively(instance, LayerMask.NameToLayer("UI"));
        instance.SetActive(false);

        SettingsPopup popup = instance.GetComponent<SettingsPopup>();
        popup.EnsureAuthoringHierarchy();
        instance.SetActive(false);

        EditorUtility.SetDirty(instance);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject CreateAuthoringInstance()
    {
        GameObject root = new("SettingsPopup", typeof(RectTransform), typeof(CanvasGroup));
        SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
        ApplyStretch(root.transform as RectTransform);

        SettingsPopup popup = root.AddComponent<SettingsPopup>();
        popup.EnsureAuthoringHierarchy();
        root.SetActive(false);
        return root;
    }

    private static void ApplyStretch(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
        {
            return;
        }

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
