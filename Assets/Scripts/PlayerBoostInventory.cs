using System;
using UnityEngine;

namespace CardMesh
{
    public sealed class PlayerBoostInventory : MonoBehaviour
    {
        private const string HintCountKey = "CardMesh.Boosts.Hints";
        private const string UndoCountKey = "CardMesh.Boosts.Undo";

        [SerializeField, Min(0)] private int startingHints;
        [SerializeField, Min(0)] private int startingUndos;

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
            HintCount = Mathf.Max(0, PlayerPrefs.GetInt(HintCountKey, startingHints));
            UndoCount = Mathf.Max(0, PlayerPrefs.GetInt(UndoCountKey, startingUndos));
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
