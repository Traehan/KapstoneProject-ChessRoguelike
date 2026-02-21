using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    public class DeckManager : MonoBehaviour
    {
        public List<Card> Deck { get; private set; } = new();
        public List<Card> Discard { get; private set; } = new();
        public List<Card> Hand { get; private set; } = new();
        public List<Card> PrepHand { get; private set; } = new();

        [Header("Piece Definitions")]
        public PieceDefinition pawn;
        public PieceDefinition rook;
        public PieceDefinition knight;
        public PieceDefinition bishop;
        public PieceDefinition queen;

        public void InitializeBattleDeck()
        {
            Deck.Clear();
            Discard.Clear();
            Hand.Clear();
            PrepHand.Clear();

            for (int i = 0; i < 8; i++)
                Deck.Add(new Card(pawn));

            Deck.Add(new Card(rook));
            Deck.Add(new Card(knight));
            Deck.Add(new Card(bishop));
            Deck.Add(new Card(queen));

            Shuffle(Deck);
        }

        public void DrawPrepCards()
        {
            Debug.Log("DrawPrepCards ENTERED");
            var elites = Deck.FindAll(c => c.Definition != pawn);
            Shuffle(elites);

            for (int i = 0; i < Mathf.Min(2, elites.Count); i++)
            {
                Debug.Log("Adding prep card: " + elites[i].Definition);
                PrepHand.Add(elites[i]);
                Deck.Remove(elites[i]);
            }
        }

        public void DrawUpTo(int amount)
        {
            while (Hand.Count < amount)
            {
                if (Deck.Count == 0)
                    Reshuffle();

                if (Deck.Count == 0)
                    break;

                var card = Deck[0];
                Deck.RemoveAt(0);
                Hand.Add(card);
            }
        }

        public void DiscardRemainingHand()
        {
            Discard.AddRange(Hand);
            Hand.Clear();
        }

        public void ConsumeCard(Card card)
        {
            Debug.Log("Consumed card: " + card.Definition.displayName);
            Debug.Log("Deck remaining: " + Deck.Count);
            Debug.Log("Discard count: " + Discard.Count);

            Hand.Remove(card);
            PrepHand.Remove(card);
        }

        void Reshuffle()
        {
            if (Discard.Count == 0) return;

            Deck.AddRange(Discard);
            Discard.Clear();
            Shuffle(Deck);
        }

        void Shuffle(List<Card> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rand = Random.Range(i, list.Count);
                (list[i], list[rand]) = (list[rand], list[i]);
            }
        }
    }
}
