using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardMesh
{
    [Serializable]
    public sealed class PremiumEventPrefabReferences
    {
        [SerializeField] private GameObject windowFramePrefab;
        [SerializeField] private GameObject closeButtonPrefab;
        [SerializeField] private GameObject infoButtonPrefab;
        [SerializeField] private GameObject rewardCardPrefab;
        [SerializeField] private GameObject mainActionButtonPrefab;

        public GameObject WindowFramePrefab => windowFramePrefab;
        public GameObject CloseButtonPrefab => closeButtonPrefab;
        public GameObject InfoButtonPrefab => infoButtonPrefab;
        public GameObject RewardCardPrefab => rewardCardPrefab;
        public GameObject MainActionButtonPrefab => mainActionButtonPrefab;

#if UNITY_EDITOR
        public void AssignDefaultsIfMissing()
        {
            windowFramePrefab ??= Load("WindowFramePrefab");
            closeButtonPrefab ??= Load("CloseButtonPrefab");
            infoButtonPrefab ??= Load("InfoButtonPrefab");
            rewardCardPrefab ??= Load("RewardCardPrefab");
            mainActionButtonPrefab ??= Load("MainActionButtonPrefab");
        }

        private static GameObject Load(string prefabName)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/UI/PremiumEvents/{prefabName}.prefab");
        }
#endif
    }
}
