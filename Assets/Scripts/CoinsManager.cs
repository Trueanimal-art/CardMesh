using System;
using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class CoinsManager : MonoBehaviour
    {
        private const string CoinsKey = "CardMesh.Coins";

        [SerializeField, Min(0)] private int startingCoins;
        [SerializeField, Min(1)] private int rewardPerGoldCategory = 10;

        public int CurrentCoins { get; private set; }
        public int RewardPerGoldCategory => Mathf.Max(1, rewardPerGoldCategory);

        public event Action<int, int> CoinsChanged;

        private void Awake()
        {
            CurrentCoins = Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, startingCoins));
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
            PlayerPrefs.SetInt(CoinsKey, CurrentCoins);
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(CurrentCoins, safeAmount);
            return safeAmount;
        }

        public bool TrySpendCoins(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0)
            {
                return true;
            }

            if (CurrentCoins < safeAmount)
            {
                return false;
            }

            CurrentCoins -= safeAmount;
            PlayerPrefs.SetInt(CoinsKey, CurrentCoins);
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(CurrentCoins, -safeAmount);
            return true;
        }
    }
}
