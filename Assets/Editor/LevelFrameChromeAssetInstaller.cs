#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardMesh.EditorTools
{
    public static class LevelFrameChromeAssetInstaller
    {
        private const string SourcePath = "Assets/Resources/UI/Premium/pause_window_cardmesh.png";
        private const string OutputFolder = "Assets/Resources/UI/Premium/LevelFrame";

        [MenuItem("Tools/CardMesh/Rebuild Level Frame Chrome Assets")]
        public static void Rebuild()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
            if (texture == null)
            {
                throw new FileNotFoundException("Level Frame source texture is missing.", SourcePath);
            }

            Directory.CreateDirectory(OutputFolder);
            CreateOrReplace(texture, "level_frame_corner", new Rect(70f, 690f, 220f, 225f), new Vector2(0.5f, 0.5f));
            CreateOrReplace(texture, "level_frame_rail_h", new Rect(310f, 816f, 300f, 72f), new Vector2(0.5f, 0.5f));
            CreateOrReplace(texture, "level_frame_rail_v", new Rect(96f, 350f, 84f, 250f), new Vector2(0.5f, 0.5f));
            CreateOrReplace(texture, "level_frame_jewel", new Rect(710f, 794f, 116f, 130f), new Vector2(0.5f, 0.5f));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Level Frame chrome assets rebuilt from the approved Level Selection frame.");
        }

        private static void CreateOrReplace(Texture2D texture, string assetName, Rect rect, Vector2 pivot)
        {
            string path = $"{OutputFolder}/{assetName}.asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            Sprite sprite = Sprite.Create(texture, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
            sprite.name = assetName;
            AssetDatabase.CreateAsset(sprite, path);
        }
    }
}
#endif
