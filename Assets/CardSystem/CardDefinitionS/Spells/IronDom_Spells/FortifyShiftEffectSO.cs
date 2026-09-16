using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Fortify Shift", fileName = "FortifyShiftEffect")]
    public class FortifyShiftEffectSO : SpellEffectSO
    {
        Piece _movedPiece;
        Vector2Int _from;
        Vector2Int _to;

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.Board == null)
                return false;

            if (!(context.Target is FortifyShiftTarget target))
                return false;

            if (target.piece == null)
                return false;

            if (target.piece.Team != context.CasterTeam)
                return false;

            if (!context.Board.InBounds(target.destination))
                return false;

            if (context.Board.TryGetPiece(target.destination, out _))
                return false;

            _movedPiece = target.piece;
            _from = _movedPiece.Coord;
            _to = target.destination;

            int manhattan = Mathf.Abs(_to.x - _from.x) + Mathf.Abs(_to.y - _from.y);
            if (manhattan != 1)
                return false;

            if (!context.Board.TryMovePiece(_movedPiece, _to))
                return false;

            _movedPiece.GetComponent<PieceRuntime>()?.Notify_PieceMoved(_from, _to);
            GameEvents.OnPieceMoved?.Invoke(_movedPiece, _from, _to, MoveReason.SpellEffect);
            GameEvents.OnPieceStatsChanged?.Invoke(_movedPiece);

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (_movedPiece == null || context == null || context.Board == null)
                return;

            if (!context.Board.TryMovePiece(_movedPiece, _from))
                return;

            _movedPiece.GetComponent<PieceRuntime>()?.Notify_Undo();
            GameEvents.OnPieceMoved?.Invoke(_movedPiece, _to, _from, MoveReason.Undo);
            GameEvents.OnPieceStatsChanged?.Invoke(_movedPiece);
        }
    }
}