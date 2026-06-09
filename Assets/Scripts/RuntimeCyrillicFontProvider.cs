using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SolitaireAssociations
{
    public static class RuntimeCyrillicFontProvider
    {
        private const string RuntimeFontResourcePath = "Fonts/Roboto-Bold";
        private const string CyrillicWarmup =
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "0123456789.,:;!?-+/() xX";

        private static readonly MethodInfo TryAddCharactersMethod = typeof(TMP_FontAsset).GetMethod(
            "TryAddCharacters",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(string) },
            null);

        private static TMP_FontAsset fontAsset;

        public static TMP_FontAsset FontAsset => fontAsset ??= CreateFontAsset();

        public static void Apply(TMP_Text text, string warmupText = null)
        {
            if (text == null)
            {
                return;
            }

            TMP_FontAsset targetFont = FontAsset;
            if (targetFont == null)
            {
                return;
            }

            WarmUp(targetFont, warmupText);
            text.font = targetFont;
            text.fontSharedMaterial = targetFont.material;
            text.enableWordWrapping = true;
            text.richText = true;
        }

        public static void WarmUp(string text)
        {
            WarmUp(FontAsset, text);
        }

        private static TMP_FontAsset CreateFontAsset()
        {
            Font sourceFont = Resources.Load<Font>(RuntimeFontResourcePath);
            if (sourceFont == null)
            {
                sourceFont = LoadOsFont();
            }

            if (sourceFont == null)
            {
                Debug.LogWarning("RuntimeCyrillicFontProvider: failed to locate a font with Cyrillic support.");
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                96,
                10,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            if (asset == null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            asset.name = $"Runtime_{sourceFont.name}_Cyrillic_UI";
            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            WarmUp(asset, CyrillicWarmup);
            asset.fallbackFontAssetTable ??= new List<TMP_FontAsset>();

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null && defaultFont != asset && !asset.fallbackFontAssetTable.Contains(defaultFont))
            {
                asset.fallbackFontAssetTable.Add(defaultFont);
            }

            return asset;
        }

        private static Font LoadOsFont()
        {
            string[] candidates =
            {
                "Arial",
                "Helvetica Neue",
                "Avenir Next",
                "DejaVu Sans",
                "Noto Sans",
                "Liberation Sans"
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    Font font = Font.CreateDynamicFontFromOSFont(candidate, 96);
                    if (font != null)
                    {
                        return font;
                    }
                }
                catch (Exception)
                {
                    // Try the next platform font name.
                }
            }

            try
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void WarmUp(TMP_FontAsset asset, string text)
        {
            if (asset == null || string.IsNullOrEmpty(text) || TryAddCharactersMethod == null)
            {
                return;
            }

            TryAddCharactersMethod.Invoke(asset, new object[] { text });
        }
    }
}
