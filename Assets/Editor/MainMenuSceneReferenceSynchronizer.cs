using System.Collections.Generic;
using SolitaireAssociations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuSceneReferenceSynchronizer
{
    private const string MainMenuScenePath = "Assets/MainMenu.unity";

    [MenuItem("Solitaire/Main Menu/Synchronize Popup References")]
    public static void SynchronizePopupReferences()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

        GameObject rateUsPopup = KeepLastNamedObject(scene, "RateUsPopup");
        if (rateUsPopup != null)
        {
            AssignObjectReference<RateUsButton>("ratingPopup", rateUsPopup);
        }

        GameObject contactPopupObject = KeepLastNamedObject(scene, "ContactPopup");
        ContactPopupController contactPopup = contactPopupObject != null
            ? contactPopupObject.GetComponent<ContactPopupController>()
            : null;
        if (contactPopup != null)
        {
            AssignObjectReference<ContactSupportButton>("contactPopup", contactPopup);
        }

        KeepLastNamedObject(scene, "SettingsPopup");
        KeepLastNamedObject(scene, "LevelSelectionPopup");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static GameObject KeepLastNamedObject(Scene scene, string objectName)
    {
        GameObject keep = null;
        long keepOrder = long.MinValue;
        List<GameObject> matches = new();

        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform == null || transform.name != objectName || transform.gameObject.scene != scene)
            {
                continue;
            }

            GameObject candidate = transform.gameObject;
            matches.Add(candidate);

            long order = GetHierarchyOrder(transform);
            if (keep == null || order >= keepOrder)
            {
                keep = candidate;
                keepOrder = order;
            }
        }

        foreach (GameObject match in matches)
        {
            if (match != null && match != keep)
            {
                Object.DestroyImmediate(match);
            }
        }

        return keep;
    }

    private static void AssignObjectReference<T>(string propertyName, Object reference) where T : Component
    {
        foreach (T target in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (target == null)
            {
                continue;
            }

            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                continue;
            }

            property.objectReferenceValue = reference;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    private static long GetHierarchyOrder(Transform transform)
    {
        List<int> path = new();
        Transform current = transform;
        while (current != null)
        {
            path.Add(current.GetSiblingIndex() + 1);
            current = current.parent;
        }

        long order = 0;
        for (int index = path.Count - 1; index >= 0; index--)
        {
            order = (order * 10000L) + path[index];
        }

        return order;
    }
}
