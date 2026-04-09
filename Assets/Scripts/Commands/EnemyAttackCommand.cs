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

            if (!_board.TryGetPiece(_attackerAt, out var aNow) || aNow != _attacker) return false;
            if (!_board.TryGetPiece(_defenderAt, out var dNow) || dNow != _defender) return false;

            int atkHPBefore = _attacker.currentHP;
            int defHPBefore = _defender.currentHP;

            _tm.ResolveCombat(_attacker, _defender, attackerIsPlayer: false,
                out bool attackerDied, out bool defenderDied);

            int dmgToDef = Mathf.Max(0, defHPBefore - _defender.currentHP);
            int dmgToAtk = Mathf.Max(0, atkHPBefore - _attacker.currentHP);

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
                reason = MoveReason.Forced
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

                        if (!attackerDied)
                        {
                            bool moved = _board.TryMovePiece(_attacker, _defenderAt);
                            if (moved)
                                GameEvents.OnPieceMoved?.Invoke(_attacker, _attackerAt, _defenderAt, MoveReason.Forced);
                        }
                    });
                }
                else
                {
                    _board.CapturePiece(_defender);
                    GameEvents.OnPieceCaptured?.Invoke(_defender, _attacker, _defenderAt);

                    if (!attackerDied)
                    {
                        bool moved = _board.TryMovePiece(_attacker, _defenderAt);
                        if (moved)
                            GameEvents.OnPieceMoved?.Invoke(_attacker, _attackerAt, _defenderAt, MoveReason.Forced);
                    }
                }
            }

            if (attackerDied)
            {
                _board.CapturePiece(_attacker);
                GameEvents.OnPieceCaptured?.Invoke(_attacker, _defender, _attackerAt);
            }

            return true;
        }

        public void Undo() { }
    }
}