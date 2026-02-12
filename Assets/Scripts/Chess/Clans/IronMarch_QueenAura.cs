using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Clans/Abilities/Iron March/Queen Aura")]
    public class IronMarch_QueenAura : AbilitySO
    {
        [SerializeField] int attackBonus = 1;
        [SerializeField] Color hintColor = new Color(1f, 0.85f, 0.2f, 1f);

        bool _queenMovedThisTurn;

        public override void OnClanEquipped(ClanRuntime ctx)
        {
            _queenMovedThisTurn = false;
        }

        public override void OnBeginPlayerTurn(ClanRuntime ctx)
        {
            _queenMovedThisTurn = false;
        }

        public override void OnPieceMoved(ClanRuntime ctx, Piece piece)
        {
            if (ctx.queen != null && piece == ctx.queen)
                _queenMovedThisTurn = false;
        }

        public override void OnAttackResolved(ClanRuntime ctx, Piece attacker, Piece defender,
                                              int dmgToDef, int dmgToAtk)
        {
            // Damage is already computed by TM; we inject bonus BEFORE calc (see TM hook below).
            // This ability only needs to know queen moved state for hints, but we’ll also expose
            // a public method TM calls to get conditional attack bonus:
        }

        // TM calls this before damage calc:
        public int GetAttackBonusIfEligible(ClanRuntime ctx, Piece attacker)
        {
            if (_queenMovedThisTurn || ctx.queen == null) return 0;
            if (attacker == null || attacker.Team != ctx.playerTeam) return 0;

            var a = attacker.Coord; var q = ctx.queen.Coord;
            if (Mathf.Abs(a.x - q.x) <= 1 && Mathf.Abs(a.y - q.y) <= 1)
                return attackBonus;

            return 0;
        }

        public override bool TryGetHintTiles(ClanRuntime ctx, out List<Vector2Int> tiles, out Color color)
        {
            tiles = null; color = hintColor;
            if (_queenMovedThisTurn || ctx.queen == null) return false;

            tiles = new List<Vector2Int>(8);
            var q = ctx.queen.Coord;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = new Vector2Int(q.x + dx, q.y + dy);
                if (ctx.board.InBounds(c)) tiles.Add(c);
            }
            return tiles.Count > 0;
        }
        
        public override void OnUndo(ClanRuntime ctx, object payload)
        {
            // After undo, recompute whether the queen counts as "moved" this turn.
            // TurnManager already restored the moved set, so we can just query it.
            _queenMovedThisTurn = ctx != null
                                  && ctx.queen != null
                                  && ctx.tm != null
                                  && ctx.tm.MovedThisPlayerTurnSnapshot.Contains(ctx.queen);
        }
    }
}
