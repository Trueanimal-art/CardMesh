using System;
using UnityEngine;

namespace CardMesh
{
    public sealed class CoinsManager : MonoBehaviour
    {
        private const string CoinsKey = "CardMesh.Coins";
        private const string EconomyVersionKey = "CardMesh.EconomyVersion";
        private const int CurrentEconomyVersion = 2;

        [SerializeField, Min(0)] private int startingCoins;
        [SerializeField, Min(0)] private int levelWinReward = 50;

        public int CurrentCoins { get; private set; }
        public int LevelWinReward => Mathf.Max(0, levelWinReward);

        public event Action<int, int> CoinsChanged;

        private void Awake()
        {
            if (PlayerPrefs.GetInt(EconomyVersionKey, 0) < CurrentEconomyVersion)
            {
                CurrentCoins = Mathf.Max(0, startingCoins);
                PlayerPrefs.SetInt(CoinsKey, CurrentCoins);
                PlayerPrefs.SetInt(EconomyVersionKey, CurrentEconomyVersion);
                PlayerPrefs.Save();
                return;
            }

            CurrentCoins = Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, startingCoins));
        }

        public void ReloadFromPrefs()
        {
            CurrentCoins = Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, startingCoins));
        }

        public int AddLevelWinReward()
        {
            return AddCoins(LevelWinReward);
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
