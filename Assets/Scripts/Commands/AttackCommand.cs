using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public sealed class AttackCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;
        readonly Piece _attacker;
        readonly Piece _defender;
        readonly Vector2Int _attackerAt;
        readonly Vector2Int _defenderAt;
        readonly int _apCost;

        int _attackerHP_Before;
        int _defenderHP_Before;
        bool _defenderWasCaptured;
        bool _attackerWasCaptured;

        List<StatusController.StatusEntry> _attackerStatuses_Before;
        List<StatusController.StatusEntry> _defenderStatuses_Before;

        public AttackCommand(TurnManager tm, ChessBoard board,
            Piece attacker, Piece defender,
            Vector2Int attackerAt, Vector2Int defenderAt,
            int apCost = 1)
        {
            _tm = tm;
            _board = board;
            _attacker = attacker;
            _defender = defender;
            _attackerAt = attackerAt;
            _defenderAt = defenderAt;
            _apCost = apCost;
        }

        public bool Execute()
        {
            if (_tm == null || _board == null) return false;
            if (_attacker == null || _defender == null) return false;

            bool attackerIsPlayer = (_attacker.Team == _tm.PlayerTeam);

            if (attackerIsPlayer && _tm.Phase != TurnPhase.PlayerTurn) return false;
            if (!attackerIsPlayer && _tm.Phase != TurnPhase.EnemyTurn) return false;

            if (_board.GetPieceAt(_attackerAt) != _attacker) return false;
            if (_board.GetPieceAt(_defenderAt) != _defender) return false;

            _attackerHP_Before = _attacker.currentHP;
            _defenderHP_Before = _defender.currentHP;

            var atkSC = _attacker.GetComponent<StatusController>();
            var defSC = _defender.GetComponent<StatusController>();
            _attackerStatuses_Before = atkSC != null ? atkSC.CaptureSnapshot() : null;
            _defenderStatuses_Before = defSC != null ? defSC.CaptureSnapshot() : null;

            if (attackerIsPlayer)
            {
                if (!_tm.TrySpendAP(_apCost)) return false;
            }

            _tm.ResolveCombat(_attacker, _defender, attackerIsPlayer: attackerIsPlayer,
                out bool attackerDied, out bool defenderDied);

            int dmgToDef = Mathf.Max(0, _defenderHP_Before - _defender.currentHP);
            int dmgToAtk = Mathf.Max(0, _attackerHP_Before - _attacker.currentHP);

            _defenderWasCaptured = false;
            _attackerWasCaptured = false;

            GameEvents.OnAttackResolved?.Invoke(new AttackReport
            {
                attacker = _attacker,
                defender = _defender,
                damageToDefender = dmgToDef,
                damageToAttacker = dmgToAtk,
                attackerDied = attackerDied,
                defenderDied = defenderDied,
                bypassedFortify = false,
                attackerTeam = _attacker.Team,
                isBossAttack = false,
                reason = MoveReason.Normal
            });

            if (defenderDied)
            {
                var motion = _defender.GetComponent<PieceMotionController>();
                Vector3 attackerWorld = _attacker.transform.position;
                Vector3 defenderWorld = _defender.transform.position;
                Vector3 recoilDir = defenderWorld - attackerWorld;
                recoilDir.y = 0f;

                if (motion != null)
                {
                    motion.PlayDeathRecoilAndDissolve(defenderWorld, recoilDir, () =>
                    {
                        _board.CapturePiece(_defender);
                        GameEvents.OnPieceCaptured?.Invoke(_defender, _attacker, _defenderAt);
                    });
                }
                else
                {
                    _board.CapturePiece(_defender);
                    GameEvents.OnPieceCaptured?.Invoke(_defender, _attacker, _defenderAt);
                }

                _defenderWasCaptured = true;
            }

            if (attackerDied)
            {
                _board.CapturePiece(_attacker);
                _attackerWasCaptured = true;
                GameEvents.OnPieceCaptured?.Invoke(_attacker, _defender, _attackerAt);
            }

            return true;
        }

        public void Undo()
        {
            if (_defenderWasCaptured)
                _board.RestoreCapturedPiece(_defender, _defenderAt);

            if (_attackerWasCaptured)
                _board.RestoreCapturedPiece(_attacker, _attackerAt);

            // restore visuals for revived pieces
            _defender?.GetComponent<PieceMotionController>()?.ResetVisualState();
            _attacker?.GetComponent<PieceMotionController>()?.ResetVisualState();

            if (_attacker != null) _attacker.currentHP = _attackerHP_Before;
            if (_defender != null) _defender.currentHP = _defenderHP_Before;

            if (_attacker != null)
            {
                var sc = _attacker.GetComponent<StatusController>();
                if (sc != null) sc.RestoreSnapshot(_attackerStatuses_Before);
            }

            if (_defender != null)
            {
                var sc = _defender.GetComponent<StatusController>();
                if (sc != null) sc.RestoreSnapshot(_defenderStatuses_Before);
            }

            if (_attacker != null && _attacker.Team == _tm.PlayerTeam)
                _tm.RefundAP(_apCost);

            GameEvents.OnCommandUndone?.Invoke(this);
        }
    }
}