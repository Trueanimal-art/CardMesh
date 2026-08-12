using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 6000.3 on Apple Silicon can lose the editor's System Font TextCore
/// handle during a domain reload and emit two spurious HandleUtility warnings.
/// Selecting Unity's bundled Inter face avoids the broken system-font path.
/// </summary>
internal static class Unity6000TextCoreWarningWorkaround
{
    private const string EditorFontPreferenceKey = "user_editor_font";
    private const string BundledEditorFont = "Inter";

    [InitializeOnLoadMethod]
    private static void Apply()
    {
        if (Application.platform != RuntimePlatform.OSXEditor
            || !Application.unityVersion.StartsWith("6000.3."))
        {
            return;
        }

        if (!string.IsNullOrEmpty(
                EditorPrefs.GetString(EditorFontPreferenceKey, string.Empty)))
        {
            return;
        }

        EditorPrefs.SetString(EditorFontPreferenceKey, BundledEditorFont);
        Unsupported.ClearSkinCache();
        EditorApplication.delayCall += EditorUtility.RequestScriptReload;
    }
}
