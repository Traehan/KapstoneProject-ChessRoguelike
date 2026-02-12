using UnityEngine;

namespace Chess
{
    public class BossPatrolCommand : IGameCommand
    {
        readonly ChessBoard _board;
        readonly Piece _bossPiece;
        readonly BossEnemy _bossAI;

        readonly int _nextIndex;
        readonly Vector2Int _nextCoord;

        public BossPatrolCommand(ChessBoard board, Piece bossPiece, BossEnemy bossAI, int nextIndex, Vector2Int nextCoord)
        {
            _board = board;
            _bossPiece = bossPiece;
            _bossAI = bossAI;
            _nextIndex = nextIndex;
            _nextCoord = nextCoord;
        }

        public bool Execute()
        {
            if (_board == null || _bossPiece == null || _bossAI == null) return false;
            if (!_board.ContainsPiece(_bossPiece)) return false;

            // Always advance index so pattern continues even when blocked
            _bossAI.PatrolIndex = _nextIndex;

            if (!_board.InBounds(_nextCoord)) return true; // index advances, no move

            // Move only if empty
            if (_board.IsOccupied(_nextCoord)) return true;

            var from = _bossPiece.Coord;
            bool moved = _board.TryMovePiece(_bossPiece, _nextCoord);
            if (moved)
                GameEvents.OnPieceMoved?.Invoke(_bossPiece, from, _nextCoord, MoveReason.Forced);

            return true;
        }

        public void Undo() { }
    }
}