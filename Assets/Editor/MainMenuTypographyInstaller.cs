using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuTypographyInstaller
{
    private const string ScenePath = "Assets/MainMenu.unity";

    [MenuItem("Tools/CardMesh/Fix Main Menu Button Typography")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        MainMenuController.EditorApplyPremiumStyles();

        RecordAndValidateLabel("PlayButton", "ИГРАТЬ", 52f);
        RecordAndValidateLabel("LevelsButton", "УРОВНИ", 27f);
        RecordAndValidateLabel("CollectionBookButton", "КОЛЛЕКЦИЯ", 27f);
        RecordAndValidateLabel("ShopButton", "МАГАЗИН", 27f);
        RecordAndValidateLabel("SettingsButton", "НАСТРОЙКИ", 27f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Main menu mobile navigation typography validated.");
    }

    private static void RecordAndValidateLabel(string buttonName, string expectedText, float expectedMaxSize)
    {
        GameObject button = GameObject.Find(buttonName);
        TMP_Text label = button != null ? FindNamedLabel(button.transform) : null;
        if (label == null)
        {
            throw new UnityException($"MainMenuTypographyInstaller: label for {buttonName} was not found.");
        }

        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(label.rectTransform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);

        Shadow shadow = label.GetComponent<Shadow>();
        if (shadow != null)
        {
            EditorUtility.SetDirty(shadow);
            PrefabUtility.RecordPrefabInstancePropertyModifications(shadow);
        }

        Outline outline = label.GetComponent<Outline>();
        if (outline != null)
        {
            EditorUtility.SetDirty(outline);
            PrefabUtility.RecordPrefabInstancePropertyModifications(outline);
        }

        if (label.text != expectedText
            || label.enableAutoSizing
            || !Mathf.Approximately(label.fontSize, expectedMaxSize))
        {
            throw new UnityException($"MainMenuTypographyInstaller: {buttonName} typography was not applied.");
        }

        if (label.preferredWidth > label.rectTransform.rect.width + 0.5f)
        {
            throw new UnityException(
                $"MainMenuTypographyInstaller: {buttonName} text width {label.preferredWidth:F1} exceeds label rect {label.rectTransform.rect.width:F1}.");
        }
    }

    private static TMP_Text FindNamedLabel(Transform root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "Label")
            {
                return text;
            }
        }

        return null;
    }
}
