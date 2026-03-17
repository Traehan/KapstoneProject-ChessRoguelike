using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Blood Court/Damage Target Piece", fileName = "FX_DamageTargetPiece")]
    public class DamageTargetPieceEffectSO : SpellEffectSO
    {
        [Min(1)] public int damage = 1;

        Piece _target;
        int _previousHP;
        bool _wasCaptured;
        Vector2Int _capturedCoord;

        public override bool Resolve(SpellContext context)
        {
            if (context == null) return false;

            _target = context.Target as Piece;
            if (_target == null) return false;

            _previousHP = _target.currentHP;
            _wasCaptured = false;
            _capturedCoord = _target.Coord;

            _target.currentHP -= damage;

            GameEvents.OnPieceDamaged?.Invoke(_target, damage, null);
            GameEvents.OnPieceStatsChanged?.Invoke(_target);

            if (_target.currentHP <= 0)
            {
                if (context.Board == null) return false;

                context.Board.CapturePiece(_target);
                _wasCaptured = true;
            }

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (_target == null) return;

            if (_wasCaptured && context != null && context.Board != null)
                context.Board.RestoreCapturedPiece(_target, _capturedCoord);

            _target.currentHP = _previousHP;
            GameEvents.OnPieceStatsChanged?.Invoke(_target);
        }
    }
}