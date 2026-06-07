using UnityEngine;

namespace SolitaireAssociations
{
    public static class PremiumUISprites
    {
        private const string ResourceRoot = "UI/Premium/";

        private static Sprite primaryButton;
        private static Sprite secondaryButton;
        private static Sprite emeraldPanel;
        private static Sprite creamPanel;
        private static Sprite iconCircle;
        private static Sprite emeraldIconCircle;
        private static Sprite categorySlot;
        private static Sprite categoryLabel;
        private static Sprite softVignette;

        public static Sprite PrimaryButton => primaryButton ??= Load("button_primary_gold");
        public static Sprite SecondaryButton => secondaryButton ??= Load("button_secondary_emerald");
        public static Sprite EmeraldPanel => emeraldPanel ??= Load("panel_emerald");
        public static Sprite CreamPanel => creamPanel ??= Load("panel_cream_gold");
        public static Sprite IconCircle => iconCircle ??= Load("icon_circle_gold");
        public static Sprite EmeraldIconCircle => emeraldIconCircle ??= Load("icon_circle_emerald");
        public static Sprite CategorySlot => categorySlot ??= Load("category_slot_emerald");
        public static Sprite CategoryLabel => categoryLabel ??= Load("category_label_gold");
        public static Sprite SoftVignette => softVignette ??= Load("vignette_soft");

        private static Sprite Load(string assetName)
        {
            return Resources.Load<Sprite>($"{ResourceRoot}{assetName}");
        }
    }
}
