using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    /// <summary>
    /// Iron Sentence: choose an allied piece with requiredFortify+ Fortify and an enemy piece as
    /// two separate targets (see IronSentenceTarget / CardTargetingMode.AlliedPieceThenEnemyPiece).
    /// The ally loses half its current Fortify (rounded down); that much damage (capped at
    /// maxDamage) is dealt to the enemy. Iron Dominion's first pure-damage spell — damage/capture
    /// handling mirrors Blood Court's DamageTargetPieceEffectSO, undo state is tracked via
    /// SpellContext (per-cast) rather than SO instance fields, matching the rest of the Iron
    /// Dominion spell set.
    /// </summary>
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Iron Sentence", fileName = "IronSentenceEffect")]
    public class IronSentenceEffectSO : SpellEffectSO
    {
        [Min(1)] public int requiredFortify = 2;
        [Min(1)] public int maxDamage = 6;

        const string AllyKey = "IronSentence_Ally";
        const string AllyPrevFortifyKey = "IronSentence_AllyPrevFortify";
        const string EnemyKey = "IronSentence_Enemy";
        const string EnemyPrevHPKey = "IronSentence_EnemyPrevHP";
        const string EnemyCoordKey = "IronSentence_EnemyCoord";
        const string EnemyStatusKey = "IronSentence_EnemyStatus";
        const string EnemyCapturedKey = "IronSentence_EnemyCaptured";

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.Board == null)
                return false;

            if (!(context.Target is IronSentenceTarget target))
                return false;

            var ally = target.ally;
            var enemy = target.enemy;

            if (ally == null || enemy == null) return false;
            if (ally == enemy) return false;
            if (ally.Team != context.CasterTeam) return false;
            if (enemy.Team == context.CasterTeam) return false;

            int allyFortify = FortifyStatusUtility.GetFortify(ally);
            if (allyFortify < requiredFortify) return false;

            int fortifyLost = allyFortify / 2; // rounded down
            if (fortifyLost <= 0) return false;

            int damage = Mathf.Min(fortifyLost, maxDamage);

            var sc = enemy.GetComponent<StatusController>();
            var enemyStatusBefore = sc != null ? sc.CaptureSnapshot() : null;

            context.SetState(AllyKey, ally);
            context.SetState(AllyPrevFortifyKey, allyFortify);
            context.SetState(EnemyKey, enemy);
            context.SetState(EnemyPrevHPKey, enemy.currentHP);
            context.SetState(EnemyCoordKey, enemy.Coord);
            context.SetState(EnemyStatusKey, enemyStatusBefore);

            FortifyStatusUtility.SetFortify(ally, allyFortify - fortifyLost);
            GameEvents.OnPieceStatsChanged?.Invoke(ally);

            var hit = PieceDamage.Apply(enemy, damage, ally, bypassFortify: false);

            bool enemyCaptured = false;
            if (hit.died)
            {
                context.Board.CapturePiece(enemy);
                enemyCaptured = true;
            }

            context.SetState(EnemyCapturedKey, enemyCaptured);

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null)
                return;

            if (context.TryGetState<Piece>(AllyKey, out var ally) && ally != null &&
                context.TryGetState<int>(AllyPrevFortifyKey, out var allyPrevFortify))
            {
                FortifyStatusUtility.SetFortify(ally, allyPrevFortify);
                GameEvents.OnPieceStatsChanged?.Invoke(ally);
            }

            if (context.TryGetState<Piece>(EnemyKey, out var enemy) && enemy != null)
            {
                if (context.TryGetState<bool>(EnemyCapturedKey, out var wasCaptured) && wasCaptured &&
                    context.TryGetState<Vector2Int>(EnemyCoordKey, out var coord) && context.Board != null)
                {
                    context.Board.RestoreCapturedPiece(enemy, coord);
                }

                if (context.TryGetState<int>(EnemyPrevHPKey, out var prevHP))
                    enemy.currentHP = prevHP;

                if (context.TryGetState<List<StatusController.StatusEntry>>(EnemyStatusKey, out var statusBefore))
                {
                    var sc = enemy.GetComponent<StatusController>();
                    if (sc != null) sc.RestoreSnapshot(statusBefore);
                }

                GameEvents.OnPieceStatsChanged?.Invoke(enemy);
            }
        }
    }
}
