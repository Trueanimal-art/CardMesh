using System;
using UnityEngine;

namespace CardMesh
{
    public sealed class PlayerBoostInventory : MonoBehaviour
    {
        private const string HintCountKey = "CardMesh.Boosts.Hints";
        private const string UndoCountKey = "CardMesh.Boosts.Undo";
        private const string InventoryVersionKey = "CardMesh.Boosts.InventoryVersion";
        private const int CurrentInventoryVersion = 1;

        [SerializeField, Min(0)] private int startingHints = 1;
        [SerializeField, Min(0)] private int startingUndos = 1;

        public int HintCount { get; private set; }
        public int UndoCount { get; private set; }

        public event Action<int, int> BoostsChanged;

        private void Awake()
        {
            Load();
        }

        public static PlayerBoostInventory GetOrCreate(Component context = null)
        {
            PlayerBoostInventory inventory = FindFirstObjectByType<PlayerBoostInventory>(FindObjectsInactive.Include);
            if (inventory != null)
            {
                return inventory;
            }

            GameObject owner = context != null ? context.gameObject : new GameObject("PlayerBoostInventory");
            inventory = owner.GetComponent<PlayerBoostInventory>();
            if (inventory == null)
            {
                inventory = owner.AddComponent<PlayerBoostInventory>();
            }

            inventory.Load();
            return inventory;
        }

        public void AddHints(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0)
            {
                return;
            }

            HintCount += safeAmount;
            Save();
        }

        public void AddUndos(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0)
            {
                return;
            }

            UndoCount += safeAmount;
            Save();
        }

        public bool TrySpendHint()
        {
            if (HintCount <= 0)
            {
                return false;
            }

            HintCount--;
            Save();
            return true;
        }

        public bool TrySpendUndo()
        {
            if (UndoCount <= 0)
            {
                return false;
            }

            UndoCount--;
            Save();
            return true;
        }

        public void DebugAddHints(int amount)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AddHints(amount);
#endif
        }

        public void DebugAddUndos(int amount)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AddUndos(amount);
#endif
        }

        private void Load()
        {
            int savedVersion = PlayerPrefs.GetInt(InventoryVersionKey, 0);
            HintCount = Mathf.Max(0, PlayerPrefs.GetInt(HintCountKey, startingHints));
            UndoCount = Mathf.Max(0, PlayerPrefs.GetInt(UndoCountKey, startingUndos));

            if (savedVersion < CurrentInventoryVersion)
            {
                // Profiles created before action counters were introduced stored zero as
                // their initial value. Grant the intended starter action once, then keep
                // all subsequent inventory changes fully persistent.
                HintCount = Mathf.Max(1, HintCount);
                UndoCount = Mathf.Max(1, UndoCount);
                PlayerPrefs.SetInt(HintCountKey, HintCount);
                PlayerPrefs.SetInt(UndoCountKey, UndoCount);
                PlayerPrefs.SetInt(InventoryVersionKey, CurrentInventoryVersion);
                PlayerPrefs.Save();
            }
        }

        private void Save()
        {
            PlayerPrefs.SetInt(HintCountKey, HintCount);
            PlayerPrefs.SetInt(UndoCountKey, UndoCount);
            PlayerPrefs.Save();
            BoostsChanged?.Invoke(HintCount, UndoCount);
        }
    }
}
