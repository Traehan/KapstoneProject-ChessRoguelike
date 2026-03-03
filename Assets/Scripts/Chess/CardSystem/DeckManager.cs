using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    public class DeckManager : MonoBehaviour
    {
        // Battle piles (reset every battle)
        public List<PieceDefinition> DrawPile { get; private set; } = new();
        public List<PieceDefinition> Hand { get; private set; } = new();
        public List<PieceDefinition> Discard { get; private set; } = new();
        public List<PieceDefinition> PlayedThisBattle { get; } = new();

        // For undo-friendly play later (Milestone 6)
        public List<PieceDefinition> PlayedThisTurn { get; private set; } = new();

        public void InitializeBattleFromRunDeck(List<PieceDefinition> runDeckNonLeaders)
        {
            DrawPile.Clear();
            Hand.Clear();
            Discard.Clear();
            PlayedThisBattle.Clear();
            PlayedThisTurn.Clear();

            if (runDeckNonLeaders == null)
            {
                Debug.LogError("[DeckManager] runDeckNonLeaders is null.");
                return;
            }

            DrawPile.AddRange(runDeckNonLeaders);
            Shuffle(DrawPile);

            Debug.Log($"[DeckManager] Battle init. DrawPile={DrawPile.Count}, Hand={Hand.Count}, Discard={Discard.Count}");
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
            }
        }

        public void DiscardEndOfTurn()
        {
            // Unplayed cards
            Discard.AddRange(Hand);
            Hand.Clear();
        }

        void ReshuffleDiscardIntoDraw()
        {
            if (Discard.Count == 0) return;

            DrawPile.AddRange(Discard);
            Discard.Clear();
            Shuffle(DrawPile);

            Debug.Log($"[DeckManager] Reshuffled. DrawPile={DrawPile.Count}");
        }

        void Shuffle(List<PieceDefinition> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rand = Random.Range(i, list.Count);
                (list[i], list[rand]) = (list[rand], list[i]);
            }
        }
    }
}