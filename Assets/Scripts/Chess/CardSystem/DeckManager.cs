using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    public class DeckManager : MonoBehaviour
    {
        // Battle piles (reset every battle)
        public List<Card> DrawPile { get; private set; } = new();
        public List<Card> Hand { get; private set; } = new();
        public List<Card> Discard { get; private set; } = new();
        public List<Card> PlayedThisBattle { get; } = new();
        public List<Card> PlayedThisTurn { get; private set; } = new();

        /// <summary>
        /// Temporary compatibility entrypoint so your current run deck can still be
        /// a List<PieceDefinition> while we transition to true card definitions.
        /// </summary>
        public void InitializeBattleFromRunDeck(List<PieceDefinition> runDeckNonLeaders)
        {
            ClearAllPilesSilently();

            if (runDeckNonLeaders == null)
            {
                Debug.LogError("[DeckManager] runDeckNonLeaders is null.");
                return;
            }

            foreach (var piece in runDeckNonLeaders)
            {
                if (piece == null) continue;

                var card = new Card(piece, manaCost: 1);
                DrawPile.Add(card);
                GameEvents.OnCardCreated?.Invoke(card);
            }

            Shuffle(DrawPile);

            Debug.Log($"[DeckManager] Battle init from legacy run deck. DrawPile={DrawPile.Count}, Hand={Hand.Count}, Discard={Discard.Count}");
        }

        /// <summary>
        /// New entrypoint for real card-definition decks.
        /// Use this once your clan/run system starts storing actual cards.
        /// </summary>
        public void InitializeBattleFromCardDefinitions(List<CardDefinitionSO> runDeck)
        {
            ClearAllPilesSilently();

            if (runDeck == null)
            {
                Debug.LogError("[DeckManager] runDeck is null.");
                return;
            }

            foreach (var def in runDeck)
            {
                if (def == null) continue;

                var card = new Card(def);
                DrawPile.Add(card);
                GameEvents.OnCardCreated?.Invoke(card);
            }

            Shuffle(DrawPile);

            Debug.Log($"[DeckManager] Battle init from card definitions. DrawPile={DrawPile.Count}, Hand={Hand.Count}, Discard={Discard.Count}");
        }

        public void DrawUpTo(int amount)
        {
            while (Hand.Count < amount)
            {
                if (DrawPile.Count == 0)
                    ReshuffleDiscardIntoDraw();

                if (DrawPile.Count == 0)
                    break;

                var top = DrawPile[0];
                DrawPile.RemoveAt(0);
                Hand.Add(top);

                GameEvents.OnCardAddedToHand?.Invoke(top);
                GameEvents.OnCardDrawn?.Invoke(top);
            }
        }

        public void DiscardEndOfTurn()
        {
            if (Hand.Count == 0)
            {
                PlayedThisTurn.Clear();
                return;
            }

            var cardsToDiscard = new List<Card>(Hand);

            foreach (var card in cardsToDiscard)
            {
                GameEvents.OnCardRemovedFromHand?.Invoke(card);
                Discard.Add(card);
                GameEvents.OnCardDiscarded?.Invoke(card);
            }

            Hand.Clear();
            PlayedThisTurn.Clear();
        }

        public bool IsInHand(Card card)
        {
            return card != null && Hand.Contains(card);
        }

        public bool RemoveFromHand(Card card)
        {
            if (card == null) return false;

            bool removed = Hand.Remove(card);
            if (removed)
                GameEvents.OnCardRemovedFromHand?.Invoke(card);

            return removed;
        }

        public void MoveToDiscard(Card card)
        {
            if (card == null) return;

            if (!Discard.Contains(card))
            {
                Discard.Add(card);
                GameEvents.OnCardDiscarded?.Invoke(card);
            }
        }

        public void MoveToPlayedThisBattle(Card card)
        {
            if (card == null) return;

            if (!PlayedThisBattle.Contains(card))
                PlayedThisBattle.Add(card);

            if (!PlayedThisTurn.Contains(card))
                PlayedThisTurn.Add(card);
        }

        public void ReturnToHand(Card card)
        {
            if (card == null) return;

            if (!Hand.Contains(card))
            {
                Hand.Add(card);
                GameEvents.OnCardAddedToHand?.Invoke(card);
                GameEvents.OnCardReturnedToHand?.Invoke(card);
            }
        }

        public void RemoveFromPlayed(Card card)
        {
            if (card == null) return;
            PlayedThisBattle.Remove(card);
            PlayedThisTurn.Remove(card);
        }

        public void Exhaust(Card card)
        {
            if (card == null) return;
            GameEvents.OnCardExhausted?.Invoke(card);
        }

        void ReshuffleDiscardIntoDraw()
        {
            if (Discard.Count == 0) return;

            DrawPile.AddRange(Discard);
            Discard.Clear();
            Shuffle(DrawPile);

            Debug.Log($"[DeckManager] Reshuffled. DrawPile={DrawPile.Count}");
        }

        void ClearAllPilesSilently()
        {
            DrawPile.Clear();
            Hand.Clear();
            Discard.Clear();
            PlayedThisBattle.Clear();
            PlayedThisTurn.Clear();
        }

        void Shuffle(List<Card> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rand = Random.Range(i, list.Count);
                (list[i], list[rand]) = (list[rand], list[i]);
            }
        }

        public bool RemoveFromDiscard(Card card)
        {
            if (card == null) return false;
            return Discard.Remove(card);
        }
        
    }
}