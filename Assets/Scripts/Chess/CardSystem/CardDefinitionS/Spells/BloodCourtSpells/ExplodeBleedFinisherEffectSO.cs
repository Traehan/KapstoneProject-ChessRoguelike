using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Blood Court/Explode Bleed Finisher", fileName = "FX_ExplodeBleedFinisher")]
    public class ExplodeBleedFinisherEffectSO : SpellEffectSO
    {
        Piece _target;
        StatusController _targetStatus;
        int _targetPreviousHP;
        bool _capturedTarget;
        Vector2Int _targetCoord;
        int _bleedUsed;
        int _spreadBleed;

        readonly List<Piece> _spreadTargets = new();
        readonly List<List<StatusController.StatusEntry>> _spreadSnapshots = new();

        static readonly Vector2Int[] AdjacentOffsets =
        {
            new Vector2Int(-1, -1),
            new Vector2Int( 0, -1),
            new Vector2Int( 1, -1),
            new Vector2Int(-1,  0),
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  1),
            new Vector2Int( 0,  1),
            new Vector2Int( 1,  1),
        };

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.Board == null)
                return false;

            _spreadTargets.Clear();
            _spreadSnapshots.Clear();
            _capturedTarget = false;
            _bleedUsed = 0;
            _spreadBleed = 0;

            _target = context.TargetPiece;
            if (_target == null)
                return false;

            _targetStatus = _target.GetComponent<StatusController>();
            if (_targetStatus == null)
                return false;

            _targetPreviousHP = _target.currentHP;
            _targetCoord = _target.Coord;

            _bleedUsed = _targetStatus.GetStacks(StatusId.Bleed);
            if (_bleedUsed <= 0)
                return true; // legal cast, just no effect

            _target.currentHP -= _bleedUsed;
            GameEvents.OnPieceDamaged?.Invoke(_target, _bleedUsed, null);
            GameEvents.OnPieceStatsChanged?.Invoke(_target);

            if (_target.currentHP > 0)
                return true;

            // Kill the main target with soft-capture so spell undo still works.
            _capturedTarget = context.Board.CapturePiece(_target);
            if (_capturedTarget)
                GameEvents.OnPieceCaptured?.Invoke(_target, null, _targetCoord);

            _spreadBleed = _bleedUsed / 2;
            if (_spreadBleed <= 0)
                return true;

            for (int i = 0; i < AdjacentOffsets.Length; i++)
            {
                Vector2Int coord = _targetCoord + AdjacentOffsets[i];
                if (!context.Board.InBounds(coord))
                    continue;

                var piece = context.Board.GetPiece(coord);
                if (piece == null)
                    continue;

                // Only spread to enemy pieces relative to the original target.
                if (piece.Team != _target.Team)
                    continue;

                var sc = piece.GetComponent<StatusController>();
                if (sc == null)
                    continue;

                // _spreadTargets.Add(piece);
                // _spreadSnapshots.Add(sc.CaptureSnapshot());

                sc.AddStacks(StatusId.Bleed, _spreadBleed);
            }

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null || context.Board == null)
                return;

            // Undo spread bleed first.
            for (int i = 0; i < _spreadTargets.Count; i++)
            {
                var piece = _spreadTargets[i];
                if (piece == null)
                    continue;

                var sc = piece.GetComponent<StatusController>();
                if (sc == null)
                    continue;

                var snapshot = i < _spreadSnapshots.Count ? _spreadSnapshots[i] : null;
                sc.RestoreSnapshot(snapshot);
            }

            _spreadTargets.Clear();
            _spreadSnapshots.Clear();

            if (_target == null)
                return;

            if (_capturedTarget)
            {
                context.Board.RestoreCapturedPiece(_target, _targetCoord);
                GameEvents.OnPieceRestored?.Invoke(_target, _targetCoord);
            }

            _target.currentHP = _targetPreviousHP;
            GameEvents.OnPieceStatsChanged?.Invoke(_target);

            _capturedTarget = false;
            _bleedUsed = 0;
            _spreadBleed = 0;
        }
    }
}