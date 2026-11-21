using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardMesh
{
    // Saves the board of the level in progress so that leaving the game
    // (closing the app, going to the menu) does not reset the level.
    public sealed partial class GameManager
    {
        private const string SessionKey = "CardMesh.Session.Board";
        private const int SessionVersion = 1;

        private bool resumeSessionOnNextRestart;
        private bool sessionDirty;
        private int sessionLevelNumber;

        [Serializable]
        private sealed class IndexList
        {
            public List<int> items = new();
        }

        [Serializable]
        private sealed class SessionCardState
        {
            public bool active;
            public bool faceUp;
            public bool stacked;
        }

        [Serializable]
        private sealed class SessionSave
        {
            public int version;
            public int level;
            public int seed;
            public string layoutId;
            public List<string> cardIds = new();
            public List<SessionCardState> cards = new();
            public List<IndexList> columns = new();
            public List<IndexList> goals = new();
            public List<int> queued = new();
            public List<int> revealed = new();
            public List<string> completed = new();
            public List<string> credited = new();
            public int remainingMoves;
            public bool usedHint;
            public bool usedUndo;
        }

        private void LateUpdate()
        {
            if (sessionDirty && draggedCard == null)
            {
                SaveSessionNow();
            }
        }

        private void OnDisable()
        {
            SaveSessionNow();
        }

        private void MarkSessionDirty()
        {
            sessionDirty = true;
        }

        private bool CanPersistSession =>
            !devOverrideLevelNumber.HasValue
            && !devOverrideTargetColumns.HasValue
            && generatedLevelData != null
            && spawnedCards.Count > 0;

        private void SaveSessionNow()
        {
            sessionDirty = false;
            if (!CanPersistSession)
            {
                return;
            }

            if (levelFinished || levelCompletionInProgress)
            {
                ClearSavedSession();
                return;
            }

            BoardSnapshot snapshot = CaptureBoardSnapshot();
            Dictionary<CardView, int> indexOf = new();
            SessionSave save = new()
            {
                version = SessionVersion,
                level = sessionLevelNumber,
                seed = generatedLevelData.GenerationSeed,
                layoutId = generatedLevelData.LayoutId,
                remainingMoves = snapshot.RemainingMoves,
                usedHint = levelUsedHint,
                usedUndo = levelUsedUndo,
                completed = new List<string>(snapshot.CompletedAssociations),
                credited = new List<string>(collectionCreditedAssociations)
            };

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                CardView card = spawnedCards[i];
                save.cardIds.Add(card != null && card.Data != null ? card.Data.Id : string.Empty);
                CardSnapshot state = card != null && snapshot.CardStates.TryGetValue(card, out CardSnapshot s) ? s : null;
                save.cards.Add(new SessionCardState
                {
                    active = state?.IsActive ?? false,
                    faceUp = state?.IsFaceUp ?? false,
                    stacked = state?.IsPlayerStackedOnPrevious ?? false
                });
                if (card != null)
                {
                    indexOf[card] = i;
                }
            }

            foreach (List<CardView> column in snapshot.Columns)
            {
                save.columns.Add(ToIndices(column, indexOf));
            }

            foreach (List<CardView> goal in snapshot.GoalSlots)
            {
                save.goals.Add(ToIndices(goal, indexOf));
            }

            save.queued = ToIndices(snapshot.QueuedDrawCards, indexOf).items;
            save.revealed = ToIndices(snapshot.RevealedDrawCards, indexOf).items;

            PlayerPrefs.SetString(SessionKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private static IndexList ToIndices(List<CardView> cards, Dictionary<CardView, int> indexOf)
        {
            IndexList list = new();
            if (cards == null)
            {
                return list;
            }

            foreach (CardView card in cards)
            {
                if (card != null && indexOf.TryGetValue(card, out int index))
                {
                    list.items.Add(index);
                }
            }

            return list;
        }

        private static void ClearSavedSession()
        {
            if (PlayerPrefs.HasKey(SessionKey))
            {
                PlayerPrefs.DeleteKey(SessionKey);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Called right after a fresh board for <paramref name="levelNumber"/> was dealt.
        /// Restores the saved board when it belongs to exactly this deal.
        /// </summary>
        private bool TryResumeSavedSession(int levelNumber, int extraMovesGrantedNow)
        {
            sessionLevelNumber = levelNumber;
            bool allowResume = resumeSessionOnNextRestart;
            resumeSessionOnNextRestart = false;

            if (!CanPersistSession)
            {
                return false;
            }

            string json = PlayerPrefs.GetString(SessionKey, string.Empty);
            if (!allowResume || string.IsNullOrEmpty(json))
            {
                ClearSavedSession();
                return false;
            }

            SessionSave save;
            try
            {
                save = JsonUtility.FromJson<SessionSave>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameManager: saved session is unreadable, starting fresh. {exception.Message}");
                ClearSavedSession();
                return false;
            }

            if (save == null
                || save.version != SessionVersion
                || save.level != levelNumber
                || save.seed != generatedLevelData.GenerationSeed
                || save.layoutId != generatedLevelData.LayoutId
                || save.cardIds.Count != spawnedCards.Count
                || save.cards.Count != spawnedCards.Count
                || save.columns.Count != columns.Count
                || save.goals.Count != goalSlots.Count)
            {
                ClearSavedSession();
                return false;
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] == null || spawnedCards[i].Data == null || spawnedCards[i].Data.Id != save.cardIds[i])
                {
                    ClearSavedSession();
                    return false;
                }
            }

            if (!ValidIndices(save))
            {
                ClearSavedSession();
                return false;
            }

            BoardSnapshot snapshot = new()
            {
                RemainingMoves = save.remainingMoves + Mathf.Max(0, extraMovesGrantedNow),
                CompletedAssociations = new HashSet<string>(save.completed),
                QueuedDrawCards = FromIndices(save.queued),
                RevealedDrawCards = FromIndices(save.revealed)
            };

            foreach (IndexList column in save.columns)
            {
                snapshot.Columns.Add(FromIndices(column.items));
            }

            foreach (IndexList goal in save.goals)
            {
                snapshot.GoalSlots.Add(FromIndices(goal.items));
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                snapshot.CardStates[spawnedCards[i]] = new CardSnapshot
                {
                    IsActive = save.cards[i].active,
                    IsFaceUp = save.cards[i].faceUp,
                    IsPlayerStackedOnPrevious = save.cards[i].stacked
                };
            }

            if (!RestoreBoardSnapshot(snapshot))
            {
                ClearSavedSession();
                return false;
            }

            collectionCreditedAssociations.Clear();
            foreach (string id in save.credited)
            {
                collectionCreditedAssociations.Add(id);
            }

            levelUsedHint = save.usedHint;
            levelUsedUndo = save.usedUndo;
            UpdateMovesDisplay();
            Debug.Log($"GameManager: resumed saved board for level {levelNumber}.");
            return true;
        }

        private bool ValidIndices(SessionSave save)
        {
            HashSet<int> seen = new();
            bool Check(List<int> list)
            {
                foreach (int index in list)
                {
                    if (index < 0 || index >= spawnedCards.Count || !seen.Add(index))
                    {
                        return false;
                    }
                }

                return true;
            }

            foreach (IndexList column in save.columns)
            {
                if (!Check(column.items))
                {
                    return false;
                }
            }

            foreach (IndexList goal in save.goals)
            {
                if (!Check(goal.items))
                {
                    return false;
                }
            }

            return Check(save.queued) && Check(save.revealed);
        }

        private List<CardView> FromIndices(List<int> indices)
        {
            List<CardView> cards = new();
            foreach (int index in indices)
            {
                cards.Add(spawnedCards[index]);
            }

            return cards;
        }
    }
}
