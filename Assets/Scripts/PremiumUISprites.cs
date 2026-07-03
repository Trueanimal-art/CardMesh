using UnityEngine;

namespace SolitaireAssociations
{
    public static class PremiumUISprites
    {
        private const string ResourceRoot = "UI/Premium/";

        private static Sprite primaryButton;
        private static Sprite cleanGoldButton;
        private static Sprite gameplayActionButton;
        private static Sprite hudCounterGold;
        private static Sprite hudPanelCreamGold;
        private static Sprite secondaryButton;
        private static Sprite emeraldPanel;
        private static Sprite creamPanel;
        private static Sprite iconCircle;
        private static Sprite emeraldIconCircle;
        private static Sprite cleanIconCircle;
        private static Sprite menuButtonCardMesh;
        private static Sprite hintBulbIcon;
        private static Sprite undoIcon;
        private static Sprite boosterRocketIcon;
        private static Sprite coinIcon;
        private static Sprite categorySlot;
        private static Sprite emptyColumnSlot;
        private static Sprite categoryLabel;
        private static Sprite softVignette;
        private static Sprite pauseWindowCardMesh;

        public static Sprite PrimaryButton => primaryButton ??= Load("button_primary_gold");
        public static Sprite CleanGoldButton => cleanGoldButton ??= Load("button_gold_clean");
        public static Sprite GameplayActionButton => gameplayActionButton ??= Load("button_gameplay_action_gold");
        public static Sprite HudCounterGold => hudCounterGold ??= Load("hud_counter_gold");
        public static Sprite HudPanelCreamGold => hudPanelCreamGold ??= Load("hud_panel_cream_gold");
        public static Sprite SecondaryButton => secondaryButton ??= Load("button_secondary_emerald");
        public static Sprite EmeraldPanel => emeraldPanel ??= Load("panel_emerald");
        public static Sprite CreamPanel => creamPanel ??= Load("panel_cream_gold");
        public static Sprite IconCircle => iconCircle ??= Load("icon_circle_gold");
        public static Sprite EmeraldIconCircle => emeraldIconCircle ??= Load("icon_circle_emerald");
        public static Sprite CleanIconCircle => cleanIconCircle ??= Load("icon_circle_clean");
        public static Sprite MenuButtonCardMesh => menuButtonCardMesh ??= Load("menu_button_cardmesh");
        public static Sprite HintBulbIcon => hintBulbIcon ??= Load("icon_hint_bulb");
        public static Sprite UndoIcon => undoIcon ??= Load("icon_undo_gold");
        public static Sprite BoosterRocketIcon => boosterRocketIcon ??= Load("icon_booster_rocket");
        public static Sprite CoinIcon => coinIcon ??= Load("icon_coin_stack");
        public static Sprite CategorySlot => categorySlot ??= Load("category_slot_cardmesh");
        public static Sprite EmptyColumnSlot => emptyColumnSlot ??= Load("empty_column_slot_gray");
        public static Sprite CategoryLabel => categoryLabel ??= Load("category_label_gold");
        public static Sprite SoftVignette => softVignette ??= Load("vignette_soft");
        public static Sprite PauseWindowCardMesh => pauseWindowCardMesh ??= Load("pause_window_cardmesh");

        private static Sprite Load(string assetName)
        {
            return Resources.Load<Sprite>($"{ResourceRoot}{assetName}");
        }
    }
}
