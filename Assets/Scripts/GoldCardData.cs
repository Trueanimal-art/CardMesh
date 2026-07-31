using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardMesh
{
    [Serializable]
    public sealed class GoldCardData
    {
        [SerializeField] private string id = "gold_card_id";
        [SerializeField] private string title = "Новая категория";
        [SerializeField] private List<CardData> cards = new();

        public string Id => id ?? string.Empty;
        public string Title => title ?? string.Empty;
        public IReadOnlyList<CardData> Cards => cards;
    }
}
