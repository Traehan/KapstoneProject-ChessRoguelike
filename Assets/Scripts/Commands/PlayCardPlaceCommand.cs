using UnityEngine;
using Card;

namespace Chess
{
    public class PlayCardPlaceCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;
        readonly PlacementManager _placer;
        readonly DeckManager _deck;
        readonly PieceDefinition _def;
        readonly Vector2Int _coord;
        readonly int _apCost;

        Piece _placedInstance;

        public PlayCardPlaceCommand(
            TurnManager tm,
            ChessBoard board,
            PlacementManager placer,
            DeckManager deck,
            PieceDefinition def,
            Vector2Int coord,
            int apCost = 1)
        {
            _tm = tm;
            _board = board;
            _placer = placer;
            _deck = deck;
            _def = def;
            _coord = coord;
            _apCost = apCost;
        }

        public bool Execute()
        {
            if (_tm == null || _board == null || _placer == null || _deck == null || _def == null) return false;
            if (_tm.Phase != TurnPhase.PlayerTurn) return false;
            if (!_board.InBounds(_coord)) return false;

            // must be in hand
            if (!_deck.Hand.Contains(_def)) return false;

            if (!_tm.TrySpendAP(_apCost)) return false;

            // place
            if (!_placer.TryPlace(_def, _coord))
            {
                _tm.RefundAP(_apCost);
                return false;
            }

            // find placed instance
            if (!_board.TryGetPiece(_coord, out _placedInstance) || _placedInstance == null)
            {
                _tm.RefundAP(_apCost);
                return false;
            }

            // Hand -> Exhausted/PlayedThisBattle
            _deck.Hand.Remove(_def);

            // Use whichever list name you actually have:
            // _deck.ExhaustedThisBattle.Add(_def);
            _deck.PlayedThisBattle.Add(_def);

            return true;
        }

        public void Undo()
        {
            if (_tm == null || _board == null || _deck == null || _def == null) return;

            // remove the spawned piece
            if (_placedInstance != null)
            {
                if (!_board.TryRemovePieceAt(_placedInstance.Coord))
                    Object.Destroy(_placedInstance.gameObject);
            }
            else
            {
                _board.TryRemovePieceAt(_coord);
            }

            // refund AP
            _tm.RefundAP(_apCost);

            // return card to hand
            _deck.PlayedThisBattle.Remove(_def);
            if (!_deck.Hand.Contains(_def))
                _deck.Hand.Add(_def);
        }
    }
}