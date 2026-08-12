#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardMesh.EditorTools
{
    internal static class ConsoleDiagnostics
    {
        private static readonly Regex ScriptGuidPattern = new(
            @"m_Script:.*guid: ([0-9a-f]{32})",
            RegexOptions.Compiled);

        [MenuItem("Tools/Card Mesh/Diagnostics/Clear Console")]
        private static void ClearConsole()
        {
            Type logEntriesType = Type.GetType(
                "UnityEditor.LogEntries, UnityEditor.CoreModule");
            MethodInfo clearMethod = logEntriesType?.GetMethod(
                "Clear",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            clearMethod?.Invoke(null, null);
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("Tools/Card Mesh/Diagnostics/Report Missing Scripts")]
        private static void ReportMissingScripts()
        {
            int affectedObjects = 0;
            int missingComponents = 0;
            foreach (GameObject target in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (target == null
                    || !target.scene.IsValid()
                    || !target.scene.isLoaded)
                {
                    continue;
                }

                int count =
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);
                if (count <= 0)
                {
                    continue;
                }

                affectedObjects++;
                missingComponents += count;
                Debug.Log(
                    $"Missing script diagnostic: scene={target.scene.path}; "
                    + $"object={GetPath(target.transform)}; count={count}; "
                    + $"prefab={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target)}",
                    target);
            }

            Debug.Log(
                $"Missing script diagnostic completed: "
                + $"objects={affectedObjects}, components={missingComponents}.");
        }

        [MenuItem("Tools/Card Mesh/Diagnostics/Report Project Missing Scripts")]
        private static void ReportProjectMissingScripts()
        {
            int affectedObjects = 0;
            int missingComponents = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                ScanHierarchy(
                    prefab != null ? prefab.transform : null,
                    path,
                    ref affectedObjects,
                    ref missingComponents);
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        ScanHierarchy(
                            root.transform,
                            path,
                            ref affectedObjects,
                            ref missingComponents);
                    }
                }
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }

            Debug.Log(
                $"Project missing script diagnostic completed: "
                + $"objects={affectedObjects}, components={missingComponents}.");
        }

        [MenuItem("Tools/Card Mesh/Diagnostics/Report Unresolved Script GUIDs")]
        private static void ReportUnresolvedScriptGuids()
        {
            HashSet<string> reported = new();
            int unresolvedCount = 0;
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                bool isProjectOrPackageAsset =
                    path.StartsWith("Assets/", StringComparison.Ordinal)
                    || path.StartsWith("Packages/", StringComparison.Ordinal);
                if (!isProjectOrPackageAsset
                    || (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                        && !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                        && !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string contents;
                try
                {
                    contents = File.ReadAllText(path);
                }
                catch
                {
                    continue;
                }

                foreach (Match match in ScriptGuidPattern.Matches(contents))
                {
                    string guid = match.Groups[1].Value;
                    if (guid.StartsWith("0000000000000000", StringComparison.Ordinal)
                        || !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        continue;
                    }

                    string key = $"{path}|{guid}";
                    if (!reported.Add(key))
                    {
                        continue;
                    }

                    unresolvedCount++;
                    Debug.Log($"Unresolved script GUID: asset={path}; guid={guid}");
                }
            }

            Debug.Log(
                $"Unresolved script GUID diagnostic completed: "
                + $"references={unresolvedCount}.");
        }

        private static void ScanHierarchy(
            Transform root,
            string assetPath,
            ref int affectedObjects,
            ref int missingComponents)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
            {
                int count =
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        target.gameObject);
                if (count <= 0)
                {
                    continue;
                }

                affectedObjects++;
                missingComponents += count;
                Debug.Log(
                    $"Project missing script: asset={assetPath}; "
                    + $"object={GetPath(target)}; count={count}",
                    target.gameObject);
            }
        }

        private static string GetPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = $"{target.name}/{path}";
            }

            return path;
        }
    }
}
#endif
