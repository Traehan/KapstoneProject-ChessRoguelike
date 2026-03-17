using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Blood Court/Destroy Target Piece", fileName = "FX_DestroyTargetPiece")]
    public class DestroyTargetPieceEffectSO : SpellEffectSO
    {
        Piece _target;
        Vector2Int _capturedCoord;
        bool _captured;

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.Board == null)
                return false;

            _target = context.Target as Piece;
            if (_target == null)
                return false;

            _capturedCoord = _target.Coord;
            _captured = context.Board.CapturePiece(_target);

            if (_captured)
            {
                GameEvents.OnPieceCaptured?.Invoke(_target, null, _capturedCoord);
                GameEvents.OnPieceStatsChanged?.Invoke(_target);
            }

            return _captured;
        }

        public override void Undo(SpellContext context)
        {
            if (!_captured || _target == null || context == null || context.Board == null)
                return;

            context.Board.RestoreCapturedPiece(_target, _capturedCoord);
            GameEvents.OnPieceRestored?.Invoke(_target, _capturedCoord);
            GameEvents.OnPieceStatsChanged?.Invoke(_target);
        }
    }
}