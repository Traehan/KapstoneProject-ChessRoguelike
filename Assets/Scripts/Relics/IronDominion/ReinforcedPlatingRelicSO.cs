using System.Linq;
using UnityEngine;

namespace Chess
{
    // Formerly the Iron Dominion clan passive (IronMarch_FortifyEndTurn). Now an optional
    // relic pick instead of a guaranteed default: any piece that didn't move this turn
    // gains Fortify at end of turn.
    [CreateAssetMenu(menuName = "Relics/Iron Dominion/Reinforced Plating")]
    public class ReinforcedPlatingRelicSO : RelicSO
    {
        [SerializeField, Min(1)] int fortifyPerTurn = 1;
        [SerializeField, Min(1)] int maxStacks = 100;

        public override void OnEndPlayerTurn(ClanRuntime ctx)
        {
            var moved = ctx.tm.MovedThisPlayerTurnSnapshot;

            foreach (var p in ctx.board.GetAllPieces().Where(p => p != null && p.Team == ctx.playerTeam))
            {
                if (!moved.Contains(p))
                    FortifyStatusUtility.AddFortify(p, fortifyPerTurn, maxStacks);
            }
        }
    }
}
