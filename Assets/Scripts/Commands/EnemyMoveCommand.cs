using UnityEngine;

namespace Chess
{
    // Enemy move with NO AP cost and NO history tracking.
    // Still fires GameEvents so UI/FX/abilities stay consistent.
    public class EnemyMoveCommand : IGameCommand
    {
        readonly ChessBoard _board;
        readonly Piece _mover;
        readonly Vector2Int _from;
        readonly Vector2Int _to;

        public EnemyMoveCommand(ChessBoard board, Piece mover, Vector2Int from, Vector2Int to)
        {
            _board = board;
            _mover = mover;
            _from = from;
            _to = to;
        }

        public bool Execute()
        {
            if (_board == null || _mover == null) return false;
            if (!_board.InBounds(_to)) return false;

            // destination must be empty for a pure move
            if (_board.TryGetPiece(_to, out var there) && there != null) return false;

            if (!_board.TryMovePiece(_mover, _to)) return false;

            // Enemy move events
            GameEvents.OnPieceMoved?.Invoke(_mover, _from, _to, MoveReason.Forced);
            return true;
        }

        // Enemy actions are not undoable; keep method for interface completeness.
        public void Undo() { }
    }
}