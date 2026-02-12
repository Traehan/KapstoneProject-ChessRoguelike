using UnityEngine;

namespace Chess
{
    public class EnemyAttackCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;

        readonly Piece _attacker;
        readonly Piece _defender;

        readonly Vector2Int _attackerAt;
        readonly Vector2Int _defenderAt;

        public EnemyAttackCommand(TurnManager tm, ChessBoard board, Piece attacker, Piece defender,
            Vector2Int attackerAt, Vector2Int defenderAt)
        {
            _tm = tm;
            _board = board;
            _attacker = attacker;
            _defender = defender;
            _attackerAt = attackerAt;
            _defenderAt = defenderAt;
        }

        public bool Execute()
        {
            if (_tm == null || _board == null) return false;
            if (_attacker == null || _defender == null) return false;

            // both must still be on the expected tiles
            if (!_board.TryGetPiece(_attackerAt, out var aNow) || aNow != _attacker) return false;
            if (!_board.TryGetPiece(_defenderAt, out var dNow) || dNow != _defender) return false;

            int atkHPBefore = _attacker.currentHP;
            int defHPBefore = _defender.currentHP;

            _tm.ResolveCombat(_attacker, _defender, attackerIsPlayer: false,
                out bool attackerDied, out bool defenderDied);

            int dmgToDef = Mathf.Max(0, defHPBefore - _defender.currentHP);
            int dmgToAtk = Mathf.Max(0, atkHPBefore - _attacker.currentHP);

            if (dmgToDef > 0) GameEvents.OnPieceDamaged?.Invoke(_defender, dmgToDef, _attacker);
            if (dmgToAtk > 0) GameEvents.OnPieceDamaged?.Invoke(_attacker, dmgToAtk, _defender);

            // Handle deaths via soft-capture
            if (defenderDied)
            {
                _board.CapturePiece(_defender);
                GameEvents.OnPieceCaptured?.Invoke(_defender, _attacker, _defenderAt);
            }

            if (attackerDied)
            {
                _board.CapturePiece(_attacker);
                GameEvents.OnPieceCaptured?.Invoke(_attacker, _defender, _attackerAt);
            }

            // If defender died and attacker survived -> step into tile
            if (defenderDied && !attackerDied)
            {
                // Destination should now be empty (defender captured)
                bool moved = _board.TryMovePiece(_attacker, _defenderAt);
                if (moved)
                    GameEvents.OnPieceMoved?.Invoke(_attacker, _attackerAt, _defenderAt, MoveReason.Forced);
            }

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
            return true;
        }

        public void Undo() { }
    }
}
