using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Boss one-way attack:
    /// - Defender takes damage (boss takes none)
    /// - If defender dies: soft-capture (deactivate)
    /// - Fires OnPieceDamaged / OnAttackResolved / OnPieceCaptured
    /// </summary>
    public class BossRayAttackCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;
        readonly Piece _boss;
        readonly Vector2Int _targetCoord;

        public BossRayAttackCommand(TurnManager tm, ChessBoard board, Piece boss, Vector2Int targetCoord)
        {
            _tm = tm;
            _board = board;
            _boss = boss;
            _targetCoord = targetCoord;
        }

        public bool Execute()
        {
            if (_tm == null || _board == null || _boss == null) return false;
            if (!_board.InBounds(_targetCoord)) return false;
            if (!_board.ContainsPiece(_boss)) return false;

            if (!_board.TryGetPiece(_targetCoord, out var target) || target == null) return false;
            if (target.Team == _boss.Team) return false;

            int hpBefore = target.currentHP;

            _tm.ResolveBossAttack(_boss, target, out bool defenderDied);

            int dmgToDef = Mathf.Max(0, hpBefore - target.currentHP);
            if (dmgToDef > 0)
                GameEvents.OnPieceDamaged?.Invoke(target, dmgToDef, _boss);

            if (defenderDied)
            {
                // soft capture (keeps object alive)
                _board.CapturePiece(target);
                GameEvents.OnPieceCaptured?.Invoke(target, _boss, _targetCoord);
            }

            var report = new AttackReport
            {
                attacker = _boss,
                defender = target,
                damageToDefender = dmgToDef,
                damageToAttacker = 0,
                attackerDied = false,
                defenderDied = defenderDied,
                bypassedFortify = false
            };

            GameEvents.OnAttackResolved?.Invoke(report);
            return true;
        }

        // Boss actions are not undoable.
        public void Undo() { }
    }
}
