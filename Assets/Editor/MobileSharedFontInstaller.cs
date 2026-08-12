#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.SceneManagement;

internal static class MobileSharedFontInstaller
{
    private const string SourceFontPath = "Assets/Resources/Fonts/Roboto-Bold.ttf";
    private const string TargetFontPath = "Assets/Resources/Fonts/Roboto-Bold Cyrillic SDF.asset";
    private const string FallbackFontPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string AutoBuildSessionKey = "CardMesh.MobileSharedFontInstaller.AutoBuild";

    [InitializeOnLoadMethod]
    private static void BuildOnceWhenMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath) != null
            || SessionState.GetBool(AutoBuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);
        EditorApplication.delayCall += RebuildSharedCyrillicFont;
    }

    [MenuItem("Tools/Card Mesh/Mobile/Rebuild Shared Cyrillic Font %#y")]
    public static void RebuildSharedCyrillicFont()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"Shared Cyrillic font source was not found at {SourceFontPath}.");
            return;
        }

        TMP_FontAsset previousFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath);
        TMP_FontAsset generatedFont = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (generatedFont == null)
        {
            Debug.LogError("TMP failed to generate the shared Cyrillic font asset.");
            return;
        }

        generatedFont.name = "Roboto-Bold Cyrillic SDF";
        bool allCharactersAdded = generatedFont.TryAddCharacters(BuildCharacterSet());
        generatedFont.atlasPopulationMode = AtlasPopulationMode.Static;
        generatedFont.ReadFontAssetDefinition();

        if (previousFont != null)
        {
            AssetDatabase.DeleteAsset(TargetFontPath);
        }

        AssetDatabase.CreateAsset(generatedFont, TargetFontPath);
        if (generatedFont.material != null && !AssetDatabase.Contains(generatedFont.material))
        {
            AssetDatabase.AddObjectToAsset(generatedFont.material, generatedFont);
        }

        foreach (Texture2D atlasTexture in generatedFont.atlasTextures)
        {
            if (atlasTexture != null && !AssetDatabase.Contains(atlasTexture))
            {
                AssetDatabase.AddObjectToAsset(atlasTexture, generatedFont);
            }
        }

        EditorUtility.SetDirty(generatedFont);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(TargetFontPath, ImportAssetOptions.ForceUpdate);

        TMP_FontAsset savedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath);
        if (savedFont == null)
        {
            Debug.LogError("The shared Cyrillic font asset was generated but could not be reloaded.");
            return;
        }

        savedFont.ReadFontAssetDefinition();
        TMP_FontAsset fallbackFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
        if (fallbackFont != null)
        {
            savedFont.fallbackFontAssetTable =
                new List<TMP_FontAsset> { fallbackFont };
            EditorUtility.SetDirty(savedFont);
            AssetDatabase.SaveAssets();
        }
        AssignFontToScene("Assets/MainMenu.unity", savedFont);
        AssignFontToScene("Assets/Scenes/Main.unity", savedFont);

        string missingSummary = allCharactersAdded ? "none" : "see TMP font asset warnings";
        Debug.Log($"Shared Cyrillic font rebuilt and assigned to mobile scenes. Missing Unicode values: {missingSummary}.");
    }

    private static void AssignFontToScene(string scenePath, TMP_FontAsset font)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (TMP_Text label in labels)
        {
            label.font = font;
            label.fontSharedMaterial = font.material;
            EditorUtility.SetDirty(label);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static string BuildCharacterSet()
    {
        var characters = new HashSet<char>();
        for (char c = (char)32; c <= (char)126; c++)
        {
            characters.Add(c);
        }

        for (char c = '\u0400'; c <= '\u04FF'; c++)
        {
            characters.Add(c);
        }

        const string punctuation = "№₽–—…«»„“”’";
        foreach (char c in punctuation)
        {
            characters.Add(c);
        }

        var builder = new StringBuilder(characters.Count);
        foreach (char c in characters)
        {
            builder.Append(c);
        }

        return builder.ToString();
    }
}
#endif
