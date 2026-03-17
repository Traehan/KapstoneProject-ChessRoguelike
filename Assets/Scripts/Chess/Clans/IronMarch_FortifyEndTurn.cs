using System.Linq;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Clans/Abilities/Iron March/Fortify End Turn")]
    public class IronMarch_FortifyEndTurn : AbilitySO
    {
        [SerializeField, Min(1)] int fortifyPerTurn = 1;
        [SerializeField, Min(1)] int maxStacks = 100;

        public override void OnEndPlayerTurn(ClanRuntime ctx)
        {
            var moved = ctx.tm.MovedThisPlayerTurnSnapshot;

            foreach (var p in ctx.board.GetAllPieces().Where(p => p != null && p.Team == ctx.playerTeam))
            {
                if (!moved.Contains(p))
                {
                    FortifyStatusUtility.AddFortify(p, fortifyPerTurn, maxStacks);
                    Debug.Log($"[Fortify EndTurn] Added to {p.name}");
                }
            }
        }

        public override void OnPieceMoved(ClanRuntime ctx, Piece piece)
        {
            // intentional no-op
            // normal player movement clears fortify elsewhere
            // forced/spell reposition can preserve it
        }
    }
}