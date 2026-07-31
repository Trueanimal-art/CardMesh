using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CardMesh.EditorTools
{
    public static class ImageCardImportDiagnostics
    {
        private const string ImageCardRoot = "Assets/Resources/ImageCards/Premium";
        private const string ReportPath = "Tools/ImageCardAudit/unity_import_diagnostics.csv";

        [MenuItem("Tools/Image Cards/Write Import Diagnostics")]
        public static void WriteImportDiagnostics()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));

            StringBuilder csv = new();
            csv.AppendLine("id,path,meshType,textureType,spriteMode,rect,textureRect,vertices,pivot,border");

            foreach (string path in Directory.GetFiles(ImageCardRoot, "*.png"))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                string id = Path.GetFileNameWithoutExtension(path);

                string meshType = "missing";
                if (importer != null)
                {
                    TextureImporterSettings settings = new();
                    importer.ReadTextureSettings(settings);
                    meshType = settings.spriteMeshType.ToString();
                }

                csv.Append(Escape(id)).Append(',')
                    .Append(Escape(path)).Append(',')
                    .Append(Escape(meshType)).Append(',')
                    .Append(Escape(importer != null ? importer.textureType.ToString() : "missing")).Append(',')
                    .Append(Escape(importer != null ? importer.spriteImportMode.ToString() : "missing")).Append(',')
                    .Append(Escape(sprite != null ? RectToString(sprite.rect) : "missing")).Append(',')
                    .Append(Escape(sprite != null ? RectToString(sprite.textureRect) : "missing")).Append(',')
                    .Append(Escape(sprite != null ? sprite.vertices.Length.ToString() : "missing")).Append(',')
                    .Append(Escape(sprite != null ? sprite.pivot.ToString() : "missing")).Append(',')
                    .Append(Escape(sprite != null ? sprite.border.ToString() : "missing"))
                    .AppendLine();
            }

            File.WriteAllText(ReportPath, csv.ToString(), Encoding.UTF8);
            Debug.Log($"ImageCard import diagnostics written to {ReportPath}");
        }

        [MenuItem("Tools/Image Cards/Force FullRect Import")]
        public static void ForceFullRectImport()
        {
            List<string> changed = new();
            foreach (string path in Directory.GetFiles(ImageCardRoot, "*.png"))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                TextureImporterSettings settings = new();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType == SpriteMeshType.FullRect)
                {
                    continue;
                }

                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                EditorUtility.SetDirty(importer);
                changed.Add(path);
            }

            if (changed.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string path in changed)
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                AssetDatabase.SaveAssets();
            }

            Debug.Log($"ImageCard FullRect import pass changed {changed.Count} sprites.");
            WriteImportDiagnostics();
        }

        private static string RectToString(Rect rect)
        {
            return $"{rect.x:0.###}:{rect.y:0.###}:{rect.width:0.###}:{rect.height:0.###}";
        }

        private static string Escape(string value)
        {
            value ??= string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
