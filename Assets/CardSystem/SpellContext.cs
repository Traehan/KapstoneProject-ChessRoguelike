using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    public sealed class SpellContext
    {
        public TurnManager TurnManager { get; }
        public ChessBoard Board { get; }
        public DeckManager DeckManager { get; }
        public Card Card { get; }
        public SpellCardDefinitionSO SpellDefinition { get; }
        public Team CasterTeam { get; }
        public object Target { get; }

        readonly Dictionary<string, object> _state = new();

        public SpellContext(
            TurnManager turnManager,
            ChessBoard board,
            DeckManager deckManager,
            Card card,
            SpellCardDefinitionSO spellDefinition,
            Team casterTeam,
            object target)
        {
            TurnManager = turnManager;
            Board = board;
            DeckManager = deckManager;
            Card = card;
            SpellDefinition = spellDefinition;
            CasterTeam = casterTeam;
            Target = target;
        }

        public Piece TargetPiece => Target as Piece;

        public bool TryGetTargetCoord(out Vector2Int coord)
        {
            if (Target is Vector2Int c)
            {
                coord = c;
                return true;
            }

            if (Target is Piece p)
            {
                coord = p.Coord;
                return true;
            }

            coord = default;
            return false;
        }

        public void SetState(string key, object value)
        {
            _state[key] = value;
        }

        public bool TryGetState<T>(string key, out T value)
        {
            if (_state.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }
}