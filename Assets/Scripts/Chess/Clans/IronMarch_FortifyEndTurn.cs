using System.Linq;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Clans/Abilities/Iron March/Fortify End Turn")]
    public class IronMarch_FortifyEndTurn : AbilitySO
    {
        [SerializeField, Min(1)] int maxStacks = 5;

        public override void OnEndPlayerTurn(ClanRuntime ctx)
        {
            // stationary allies are those not in the moved set (ask TM)
            var moved = ctx.tm.MovedThisPlayerTurnSnapshot; // HashSet<Piece>
            foreach (var p in ctx.board.GetAllPieces().Where(p => p != null && p.Team == ctx.playerTeam))
            {
                if (!moved.Contains(p)) p.AddFortify(maxStacks);
            }
        }

        public override void OnPieceMoved(ClanRuntime ctx, Piece piece)
        {
            if (piece != null && piece.Team == ctx.playerTeam)
                piece.ClearFortify();
        }
    }
}