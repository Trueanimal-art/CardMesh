using System;
using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class CoinsManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingCoins;
        [SerializeField, Min(1)] private int rewardPerGoldCategory = 10;

        public int CurrentCoins { get; private set; }
        public int RewardPerGoldCategory => Mathf.Max(1, rewardPerGoldCategory);

        public event Action<int, int> CoinsChanged;

        private void Awake()
        {
            CurrentCoins = Mathf.Max(0, startingCoins);
        }

        public int AddGoldCategoryReward()
        {
            return AddCoins(RewardPerGoldCategory);
        }

        public int AddCoins(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0)
            {
                return 0;
            }

            CurrentCoins += safeAmount;
            CoinsChanged?.Invoke(CurrentCoins, safeAmount);
            return safeAmount;
        }
    }
}
