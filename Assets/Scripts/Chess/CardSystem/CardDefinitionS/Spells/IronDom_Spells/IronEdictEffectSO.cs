using System.Collections.Generic;
using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Iron Edict", fileName = "IronEdictEffect")]
    public class IronEdictEffectSO : SpellEffectSO
    {
        [Min(0)] public int halfWidth = 1;
        [Min(0)] public int halfHeight = 1;
        [Min(1)] public int fortifyAmount = 1;
        [Min(1)] public int maxStacks = 20;

        [System.Serializable]
        class Snapshot
        {
            public Piece piece;
            public int previousFortify;
        }

        const string UndoKey = "IronEdict_Snapshots";

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

                snapshots.Add(new Snapshot
                {
                    piece = p,
                    previousFortify = FortifyStatusUtility.GetFortify(p)
                });

                FortifyStatusUtility.AddFortify(p, fortifyAmount, maxStacks);
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

                FortifyStatusUtility.SetFortify(s.piece, s.previousFortify);
                GameEvents.OnPieceStatsChanged?.Invoke(s.piece);
            }
        }
    }
}