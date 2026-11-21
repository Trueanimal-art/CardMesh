using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardMesh.EditorTools
{
    internal sealed class CleanRoyalPopupSpriteImporter : AssetPostprocessor
    {
        private static readonly Dictionary<string, Vector4> Borders = new()
        {
            ["daily_reward_card.png"] = new Vector4(36, 40, 36, 40),
            ["daily_reward_card_wide.png"] = new Vector4(36, 40, 36, 40),
            ["daily_reward_card_available.png"] = new Vector4(36, 40, 36, 40),
            ["daily_reward_hero.png"] = new Vector4(72, 40, 72, 40),
            ["royal_stage_idle.png"] = new Vector4(56, 56, 56, 56),
            ["royal_stage_active.png"] = new Vector4(56, 56, 56, 56),
            ["royal_goal_panel.png"] = new Vector4(40, 40, 40, 40),
            ["royal_progress_track.png"] = new Vector4(40, 40, 40, 40),
            ["collection_reward_card.png"] = new Vector4(28, 28, 28, 28),
            ["collection_progress_panel.png"] = new Vector4(30, 26, 30, 26),
            ["collection_info_panel.png"] = new Vector4(30, 30, 30, 30),
        };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/UI/CleanRoyal/")) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 200f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (Borders.TryGetValue(System.IO.Path.GetFileName(assetPath), out Vector4 border))
            {
                importer.spriteBorder = border;
            }
        }
    }
}
