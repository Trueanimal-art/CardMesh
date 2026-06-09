using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    [Serializable]
    public sealed class ChestChaseRewardTier
    {
        [SerializeField, Min(0)] private int coins;
        [SerializeField, Min(0)] private int hints;
        [SerializeField, Min(0)] private int undos;
        [SerializeField, Min(1)] private int keysRequired = 20;

        public int Coins => Mathf.Max(0, coins);
        public int Hints => Mathf.Max(0, hints);
        public int Undos => Mathf.Max(0, undos);
        public int KeysRequired => Mathf.Max(1, keysRequired);

        public ChestChaseRewardTier(int coins, int hints, int undos, int keysRequired)
        {
            this.coins = Mathf.Max(0, coins);
            this.hints = Mathf.Max(0, hints);
            this.undos = Mathf.Max(0, undos);
            this.keysRequired = Mathf.Max(1, keysRequired);
        }
    }

    [CreateAssetMenu(fileName = "ChestChaseRewardConfig", menuName = "Solitaire Associations/Rewards/Chest Chase Config")]
    public sealed class ChestChaseRewardConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int eventDurationHours = 41;
        [SerializeField] private List<ChestChaseRewardTier> tiers = new()
        {
            new ChestChaseRewardTier(300, 1, 0, 20),
            new ChestChaseRewardTier(500, 1, 1, 20),
            new ChestChaseRewardTier(700, 2, 2, 20),
            new ChestChaseRewardTier(1000, 3, 3, 20),
            new ChestChaseRewardTier(1500, 5, 5, 20)
        };

        public int EventDurationHours => Mathf.Max(1, eventDurationHours);
        public IReadOnlyList<ChestChaseRewardTier> Tiers => tiers;

        public ChestChaseRewardTier GetTier(int index)
        {
            if (tiers == null || tiers.Count == 0)
            {
                return null;
            }

            return tiers[Mathf.Clamp(index, 0, tiers.Count - 1)];
        }
    }
}
