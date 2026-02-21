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

            // Allow BOTH player-turn and enemy-turn attacks.
            // Player attacks are one-way (defender takes damage).
            // Enemy attacks are simultaneous (both can take damage).
            bool attackerIsPlayer = (_attacker.Team == _tm.PlayerTeam);

            if (attackerIsPlayer && _tm.Phase != TurnPhase.PlayerTurn) return false;
            if (!attackerIsPlayer && _tm.Phase != TurnPhase.EnemyTurn) return false;

            // Ensure still on expected cells
            if (_board.GetPieceAt(_attackerAt) != _attacker) return false;
            if (_board.GetPieceAt(_defenderAt) != _defender) return false;

            // Snapshot HP for undo
            _attackerHP_Before = _attacker.currentHP;
            _defenderHP_Before = _defender.currentHP;

            // Only player actions spend AP.
            if (attackerIsPlayer)
            {
                if (!_tm.TrySpendAP(_apCost)) return false;
            }

            // Resolve combat (this is where PieceRuntime hooks + abilities run)
            _tm.ResolveCombat(_attacker, _defender, attackerIsPlayer: attackerIsPlayer,
                out bool attackerDied, out bool defenderDied);

            // After damage, compute deltas for event report
            int dmgToDef = Mathf.Max(0, _defenderHP_Before - _defender.currentHP);
            int dmgToAtk = Mathf.Max(0, _attackerHP_Before - _attacker.currentHP);

            // Capture/remove pieces (soft capture)
            _defenderWasCaptured = false;
            _attackerWasCaptured = false;

            if (defenderDied)
            {
                _board.CapturePiece(_defender);
                _defenderWasCaptured = true;
            }

            if (attackerDied)
            {
                _board.CapturePiece(_attacker);
                _attackerWasCaptured = true;
            }

            // Fire events (optional, but nice for UI/analytics)
            var report = new AttackReport
            {
                attacker = _attacker,
                defender = _defender,
                damageToDefender = dmgToDef,
                damageToAttacker = dmgToAtk,
                attackerDied = attackerDied,
                defenderDied = defenderDied,
                bypassedFortify = false
            };

            GameEvents.OnAttackResolved?.Invoke(report);

            if (dmgToDef > 0) GameEvents.OnPieceDamaged?.Invoke(_defender, dmgToDef, _attacker);
            if (dmgToAtk > 0) GameEvents.OnPieceDamaged?.Invoke(_attacker, dmgToAtk, _defender);

            if (_defenderWasCaptured) GameEvents.OnPieceCaptured?.Invoke(_defender, _attacker, _defenderAt);
            if (_attackerWasCaptured) GameEvents.OnPieceCaptured?.Invoke(_attacker, _defender, _attackerAt);

            return true;
        }

        public void Undo()
        {
            // Restore pieces if they were captured
            if (_defenderWasCaptured)
                _board.RestoreCapturedPiece(_defender, _defenderAt);

            if (_attackerWasCaptured)
                _board.RestoreCapturedPiece(_attacker, _attackerAt);

            // Restore HP
            if (_attacker != null) _attacker.currentHP = _attackerHP_Before;
            if (_defender != null) _defender.currentHP = _defenderHP_Before;

            // Refund AP only for player actions
            if (_attacker != null && _attacker.Team == _tm.PlayerTeam)
                _tm.RefundAP(_apCost);

            // Optional event
            GameEvents.OnCommandUndone?.Invoke(this);
        }
    }
}
