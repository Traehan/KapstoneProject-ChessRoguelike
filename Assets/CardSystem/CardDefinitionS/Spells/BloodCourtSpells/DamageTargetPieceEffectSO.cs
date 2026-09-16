using System.Collections.Generic;
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
        List<StatusController.StatusEntry> _statusBefore;

        public override bool Resolve(SpellContext context)
        {
            if (context == null) return false;

            _target = context.Target as Piece;
            if (_target == null) return false;

            _previousHP = _target.currentHP;
            _wasCaptured = false;
            _capturedCoord = _target.Coord;

            var sc = _target.GetComponent<StatusController>();
            _statusBefore = sc != null ? sc.CaptureSnapshot() : null;

            var hit = PieceDamage.Apply(_target, damage, null, bypassFortify: false);

            if (hit.died)
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

            var sc = _target.GetComponent<StatusController>();
            if (sc != null) sc.RestoreSnapshot(_statusBefore);

            GameEvents.OnPieceStatsChanged?.Invoke(_target);
        }
    }
}