using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class CardDatabase : MonoBehaviour
    {
        private const string DefaultCardsResourcePath = "Data/cards";
        private const int DefaultGoalRequiredCount = 4;

        [SerializeField] private string cardsResourcePath = DefaultCardsResourcePath;
        [SerializeField, Min(1)] private int goalRequiredCount = DefaultGoalRequiredCount;

        private readonly Dictionary<string, GoldCardData> goldCardsById = new();
        private readonly Dictionary<string, RegularCardEntry> regularCardsById = new();
        private readonly List<GoldCardData> goldCards = new();

        private bool isLoaded;

        public bool IsLoaded => isLoaded;

        private void Awake()
        {
            Load();
        }

        public bool Load()
        {
            if (isLoaded)
            {
                return true;
            }

            TextAsset jsonAsset = Resources.Load<TextAsset>(cardsResourcePath);
            if (jsonAsset == null)
            {
                Debug.LogError($"CardDatabase: cards json not found at Resources/{cardsResourcePath}.json");
                return false;
            }

            CardsPayload payload = JsonUtility.FromJson<CardsPayload>(jsonAsset.text);
            if (payload == null || payload.goldCards == null || payload.goldCards.Count == 0)
            {
                Debug.LogError("CardDatabase: cards json is empty or invalid.");
                return false;
            }

            goldCardsById.Clear();
            regularCardsById.Clear();
            goldCards.Clear();

            foreach (GoldCardData goldCard in payload.goldCards)
            {
                RegisterGoldCard(goldCard);
            }

            isLoaded = goldCardsById.Count > 0;
            if (!isLoaded)
            {
                Debug.LogError("CardDatabase: no valid gold cards were loaded.");
            }

            return isLoaded;
        }

        public bool TryCreateCardInstance(string cardId, out CardData cardData)
        {
            return TryCreateCardInstance(cardId, goalRequiredCount, out cardData);
        }

        public bool TryCreateCardInstance(string cardId, int goldRequiredCountOverride, out CardData cardData)
        {
            if (!Load() || string.IsNullOrWhiteSpace(cardId))
            {
                cardData = null;
                return false;
            }

            if (goldCardsById.TryGetValue(cardId, out GoldCardData goldCard))
            {
                cardData = CardData.CreateGoldCard(goldCard, goldRequiredCountOverride);
                return cardData != null;
            }

            if (regularCardsById.TryGetValue(cardId, out RegularCardEntry regularCard))
            {
                cardData = CardData.CreateRegularCard(regularCard.Card, regularCard.GoldCard);
                return cardData != null;
            }

            cardData = null;
            return false;
        }

        public IReadOnlyList<GoldCardData> GetGoldCards()
        {
            Load();
            return goldCards;
        }

        private void RegisterGoldCard(GoldCardData goldCard)
        {
            if (goldCard == null || string.IsNullOrWhiteSpace(goldCard.Id) || string.IsNullOrWhiteSpace(goldCard.Title))
            {
                Debug.LogWarning("CardDatabase: skipped gold card with missing id or title.");
                return;
            }

            if (goldCardsById.ContainsKey(goldCard.Id) || regularCardsById.ContainsKey(goldCard.Id))
            {
                Debug.LogWarning($"CardDatabase: duplicate gold card id '{goldCard.Id}' was skipped.");
                return;
            }

            if (goldCard.Cards == null || goldCard.Cards.Count < 8 || goldCard.Cards.Count > 12)
            {
                Debug.LogWarning($"CardDatabase: gold card '{goldCard.Id}' should contain 8-12 cards.");
                return;
            }

            goldCardsById.Add(goldCard.Id, goldCard);
            goldCards.Add(goldCard);

            foreach (CardData regularCard in goldCard.Cards)
            {
                if (regularCard == null || string.IsNullOrWhiteSpace(regularCard.Id) || string.IsNullOrWhiteSpace(regularCard.Title))
                {
                    Debug.LogWarning($"CardDatabase: skipped invalid card inside gold card '{goldCard.Id}'.");
                    continue;
                }

                if (goldCardsById.ContainsKey(regularCard.Id) || regularCardsById.ContainsKey(regularCard.Id))
                {
                    Debug.LogWarning($"CardDatabase: duplicate regular card id '{regularCard.Id}' was skipped.");
                    continue;
                }

                regularCardsById.Add(regularCard.Id, new RegularCardEntry(goldCard, regularCard));
            }
        }

        [Serializable]
        private sealed class CardsPayload
        {
            public List<GoldCardData> goldCards = new();
        }

        private readonly struct RegularCardEntry
        {
            public RegularCardEntry(GoldCardData sourceGoldCard, CardData sourceCard)
            {
                GoldCard = sourceGoldCard;
                Card = sourceCard;
            }

            public GoldCardData GoldCard { get; }
            public CardData Card { get; }
        }
    }
}
