using UnityEngine;

namespace Chess
{
    public class MoveCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;
        readonly Piece _piece;
        readonly Vector2Int _from;
        readonly Vector2Int _to;
        readonly int _apCost;

        // snapshots for undo
        int _hpBefore;
        int _fortifyBefore;
        bool _pawnHasMovedBefore;

        bool _wasMarkedMovedBefore;
        bool _queenMovedThisTurnBefore;

        public MoveCommand(TurnManager tm, ChessBoard board, Piece piece, Vector2Int from, Vector2Int to, int apCost = 1)
        {
            _tm = tm;
            _board = board;
            _piece = piece;
            _from = from;
            _to = to;
            _apCost = apCost;
        }

        public bool Execute()
        {
            if (_tm == null || _board == null || _piece == null) return false;
            if (_tm.Phase != TurnPhase.PlayerTurn) return false;
            if (!_board.InBounds(_to)) return false;

            if (!_tm.TrySpendAP(_apCost)) return false;

            // snapshot
            _hpBefore = _piece.currentHP;
            _fortifyBefore = _piece.fortifyStacks;
            _pawnHasMovedBefore = (_piece is Pawn pw) && pw.hasMoved;

            _wasMarkedMovedBefore = _tm.WasMarkedMovedThisTurn(_piece);
            _queenMovedThisTurnBefore = _tm.GetQueenMovedThisTurn();

            // execute move
            if (!_board.TryMovePiece(_piece, _to))
            {
                _tm.RefundAP(_apCost);
                return false;
            }

            // gameplay bookkeeping that must be undoable
            _tm.MarkMovedThisTurn(_piece);
            _piece.ClearFortify();
            if (_tm.IsQueenLeader(_piece))
                _tm.SetQueenMovedThisTurn(true);

            _piece.GetComponent<PieceRuntime>()?.Notify_PieceMoved(_from, _to);

            GameEvents.OnPieceMoved?.Invoke(_piece, _from, _to, MoveReason.Normal);
            return true;
        }

        public void Undo()
        {
            if (_tm == null || _board == null || _piece == null) return;

            // move back
            _board.TryMovePiece(_piece, _from);

            // restore stats/flags
            _piece.currentHP = _hpBefore;
            _piece.fortifyStacks = _fortifyBefore;
            if (_piece is Pawn pw) pw.hasMoved = _pawnHasMovedBefore;

            // restore bookkeeping
            _tm.SetQueenMovedThisTurn(_queenMovedThisTurnBefore);
            if (_wasMarkedMovedBefore) _tm.MarkMovedThisTurn(_piece);
            else _tm.UnmarkMovedThisTurn(_piece);

            // refund AP
            _tm.RefundAP(_apCost);

            _piece.GetComponent<PieceRuntime>()?.Notify_Undo();

            GameEvents.OnPieceMoved?.Invoke(_piece, _to, _from, MoveReason.Undo);
        }
    }
}
