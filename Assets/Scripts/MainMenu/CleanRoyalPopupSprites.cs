using UnityEngine;

namespace CardMesh
{
    public static class CleanRoyalPopupSprites
    {
        public static Sprite DailyCard => Load("daily_reward_card");
        public static Sprite DailyCardWide => Load("daily_reward_card_wide");
        public static Sprite DailyCardAvailable => Load("daily_reward_card_available");
        public static Sprite DailyHero => Load("daily_reward_hero");
        public static Sprite RoyalStageIdle => Load("royal_stage_idle");
        public static Sprite RoyalStageActive => Load("royal_stage_active");
        public static Sprite RoyalGoal => Load("royal_goal_panel");
        public static Sprite RoyalProgress => Load("royal_progress_track");
        public static Sprite RoyalProgressFill => Load("royal_progress_fill");
        public static Sprite RoyalMilestoneDiamond => Load("royal_milestone_diamond");
        public static Sprite CollectionReward => Load("collection_reward_card");
        public static Sprite CollectionProgress => Load("collection_progress_panel");
        public static Sprite CollectionInfo => Load("collection_info_panel");
        public static Sprite GameplayHint => Resources.Load<Sprite>("UI/GameActions/icon_hint_oracle_final");
        public static Sprite GameplayUndo => Resources.Load<Sprite>("UI/GameActions/icon_undo_card_final");
        public static Sprite GameplayMoves => Resources.Load<Sprite>("UI/GameActions/icon_add_moves_cards_final");

        private static Sprite Load(string name) => Resources.Load<Sprite>($"UI/CleanRoyal/{name}");
    }
}
