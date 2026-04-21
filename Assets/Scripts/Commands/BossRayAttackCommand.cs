using UnityEngine;

namespace Chess
{
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

            var report = new AttackReport
            {
                attacker = _boss,
                defender = target,
                damageToDefender = dmgToDef,
                damageToAttacker = 0,
                attackerDied = false,
                defenderDied = defenderDied,
                bypassedFortify = false,
                attackerTeam = _boss.Team,
                isBossAttack = true,
                reason = MoveReason.Forced
            };

            GameEvents.OnAttackResolved?.Invoke(report);

            if (defenderDied)
            {
                var motion = target.GetComponent<PieceMotionController>();
                Vector3 attackerWorld = _boss.transform.position;
                Vector3 defenderWorld = target.transform.position;
                Vector3 recoilDir = defenderWorld - attackerWorld;
                recoilDir.y = 0f;

                if (motion != null)
                {
                    motion.PlayDeathRecoilAndDissolve(defenderWorld, recoilDir, () =>
                    {
                        _board.CapturePiece(target);
                        GameEvents.OnPieceCaptured?.Invoke(target, _boss, _targetCoord);
                    });
                }
                else
                {
                    _board.CapturePiece(target);
                    GameEvents.OnPieceCaptured?.Invoke(target, _boss, _targetCoord);
                }
            }

            return true;
        }

        public void Undo() { }
    }
}