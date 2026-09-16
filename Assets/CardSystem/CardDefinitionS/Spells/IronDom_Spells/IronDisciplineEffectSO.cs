using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    /// <summary>
    /// Iron Discipline: target ally and surrounding 3x3 allies each gain +1 Retaliate, capped
    /// at maxStacks each. Same AoE footprint/targeting as IronEdictEffectSO, just granting
    /// Retaliate instead of Fortify.
    /// </summary>
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Iron Discipline", fileName = "IronDisciplineEffect")]
    public class IronDisciplineEffectSO : SpellEffectSO
    {
        [Min(0)] public int halfWidth = 1;
        [Min(0)] public int halfHeight = 1;
        [Min(1)] public int retaliateAmount = 1;
        [Min(1)] public int maxStacks = 3;

        [System.Serializable]
        class Snapshot
        {
            public Piece piece;
            public int previousRetaliate;
        }

        const string UndoKey = "IronDiscipline_Snapshots";

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.Board == null)
                return false;

            if (!context.TryGetTargetCoord(out var center))
                return false;

            var snapshots = new List<Snapshot>();

            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            for (int dy = -halfHeight; dy <= halfHeight; dy++)
            {
                Vector2Int c = new Vector2Int(center.x + dx, center.y + dy);
                if (!context.Board.InBounds(c)) continue;
                if (!context.Board.TryGetPiece(c, out var p)) continue;
                if (p.Team != context.CasterTeam) continue;

                int previous = RetaliateStatusUtility.GetRetaliate(p);
                int next = Mathf.Min(previous + retaliateAmount, maxStacks);

                snapshots.Add(new Snapshot
                {
                    piece = p,
                    previousRetaliate = previous
                });

                RetaliateStatusUtility.SetRetaliate(p, next);
                GameEvents.OnPieceStatsChanged?.Invoke(p);
            }

            if (snapshots.Count == 0)
                return false;

            context.SetState(UndoKey, snapshots);
            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null)
                return;

            if (!context.TryGetState<List<Snapshot>>(UndoKey, out var snapshots) || snapshots == null)
                return;

            for (int i = 0; i < snapshots.Count; i++)
            {
                var s = snapshots[i];
                if (s == null || s.piece == null) continue;

                RetaliateStatusUtility.SetRetaliate(s.piece, s.previousRetaliate);
                GameEvents.OnPieceStatsChanged?.Invoke(s.piece);
            }
        }
    }
}
