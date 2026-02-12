using UnityEngine;

namespace Chess
{
    public class AttackCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;

        readonly Piece _attacker;
        readonly Piece _defender;

        readonly Vector2Int _attackerAt;
        readonly Vector2Int _defenderAt;
        readonly int _apCost;

        // snapshots
        int _attackerHP_Before;
        int _defenderHP_Before;
        int _attackerFortify_Before;
        int _defenderFortify_Before;
        bool _attackerPawnHadMoved_Before;

        bool _attackerWasCaptured;
        bool _defenderWasCaptured;

        public AttackCommand(TurnManager tm, ChessBoard board, Piece attacker, Piece defender,
            Vector2Int attackerAt, Vector2Int defenderAt, int apCost = 1)
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
            if (_tm.Phase != TurnPhase.PlayerTurn) return false;

            // sanity: must still be on-board at expected coords
            if (!_board.TryGetPiece(_attackerAt, out var aNow) || aNow != _attacker) return false;
            if (!_board.TryGetPiece(_defenderAt, out var dNow) || dNow != _defender) return false;

            if (!_tm.TrySpendAP(_apCost)) return false;

            // snapshot
            _attackerHP_Before = _attacker.currentHP;
            _defenderHP_Before = _defender.currentHP;
            _attackerFortify_Before = _attacker.fortifyStacks;
            _defenderFortify_Before = _defender.fortifyStacks;
            _attackerPawnHadMoved_Before = (_attacker is Pawn pw) && pw.hasMoved;

            // resolve
            _tm.ResolveCombat(_attacker, _defender, attackerIsPlayer: true,
                out bool attackerDied, out bool defenderDied);

            int dmgToDef = Mathf.Max(0, _defenderHP_Before - _defender.currentHP);
            int dmgToAtk = Mathf.Max(0, _attackerHP_Before - _attacker.currentHP);

            if (dmgToDef > 0) GameEvents.OnPieceDamaged?.Invoke(_defender, dmgToDef, _attacker);
            if (dmgToAtk > 0) GameEvents.OnPieceDamaged?.Invoke(_attacker, dmgToAtk, _defender);

            _attackerWasCaptured = false;
            _defenderWasCaptured = false;

            if (attackerDied)
            {
                _board.CapturePiece(_attacker);
                _attackerWasCaptured = true;
                GameEvents.OnPieceCaptured?.Invoke(_attacker, _defender, _attackerAt);
            }

            if (defenderDied)
            {
                _board.CapturePiece(_defender);
                _defenderWasCaptured = true;
                GameEvents.OnPieceCaptured?.Invoke(_defender, _attacker, _defenderAt);
            }

            var report = new AttackReport
            {
                attacker = _attacker,
                defender = _defender,
                damageToDefender = dmgToDef,
                damageToAttacker = dmgToAtk,
                attackerDied = attackerDied,
                defenderDied = defenderDied,
                bypassedFortify = false // set later if you expose ctx
            };

            GameEvents.OnAttackResolved?.Invoke(report);
            return true;
        }

        public void Undo()
        {
            if (_tm == null || _board == null) return;
            if (_attacker == null || _defender == null) return;

            // restore defender first
            if (_defenderWasCaptured)
            {
                _board.RestoreCapturedPiece(_defender, _defenderAt);
                GameEvents.OnPieceRestored?.Invoke(_defender, _defenderAt);
            }

            if (_attackerWasCaptured)
            {
                _board.RestoreCapturedPiece(_attacker, _attackerAt);
                GameEvents.OnPieceRestored?.Invoke(_attacker, _attackerAt);
            }

            // restore stats
            _attacker.currentHP = _attackerHP_Before;
            _defender.currentHP = _defenderHP_Before;

            _attacker.fortifyStacks = _attackerFortify_Before;
            _defender.fortifyStacks = _defenderFortify_Before;

            if (_attacker is Pawn pw)
                pw.hasMoved = _attackerPawnHadMoved_Before;

            _tm.RefundAP(_apCost);

            _attacker.GetComponent<PieceRuntime>()?.Notify_Undo();
            _defender.GetComponent<PieceRuntime>()?.Notify_Undo();
        }
    }
}
