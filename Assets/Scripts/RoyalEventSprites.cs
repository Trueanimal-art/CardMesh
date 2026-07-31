using UnityEngine;

namespace CardMesh
{
    public static class RoyalEventSprites
    {
        private const string ResourceRoot = "UI/RoyalEvent/";

        private static Sprite panel;
        private static Sprite hero;
        private static Sprite rewardCard;
        private static Sprite rewardCardActive;
        private static Sprite tooltip;
        private static Sprite progressBack;
        private static Sprite progressFill;
        private static Sprite coinIcon;
        private static Sprite buttonIcon;
        private static Sprite eventLogo;
        private static Sprite redBadge;
        private static Sprite progressMarker;

        public static Sprite Panel => panel ??= Load("royal_event_panel");
        public static Sprite Hero => hero ??= Load("royal_event_hero");
        public static Sprite RewardCard => rewardCard ??= Load("royal_reward_card");
        public static Sprite RewardCardActive => rewardCardActive ??= Load("royal_reward_card_active");
        public static Sprite Tooltip => tooltip ??= Load("royal_tooltip");
        public static Sprite ProgressBack => progressBack ??= Load("royal_progress_back");
        public static Sprite ProgressFill => progressFill ??= Load("royal_progress_fill");
        public static Sprite CoinIcon => coinIcon ??= (PremiumUISprites.CoinIcon != null ? PremiumUISprites.CoinIcon : Load("royal_icon_coin"));
        public static Sprite ButtonIcon => buttonIcon ??= Load("royal_event_button");
        public static Sprite EventLogo => eventLogo ??= Load("royal_event_logo");
        public static Sprite RedBadge => redBadge ??= Load("royal_badge_red");
        public static Sprite ProgressMarker => progressMarker ??= Load("royal_progress_marker");

        private static Sprite Load(string assetName)
        {
            return Resources.Load<Sprite>($"{ResourceRoot}{assetName}");
        }
    }
}
