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
            var moved = ctx.tm.MovedThisPlayerTurnSnapshot;
            foreach (var p in ctx.board.GetAllPieces().Where(p => p != null && p.Team == ctx.playerTeam))
            {
                if (!moved.Contains(p))
                    p.AddFortify(maxStacks);
            }
        }

        public override void OnPieceMoved(ClanRuntime ctx, Piece piece)
        {
            // Normal player movement already clears fortify inside MoveCommand.
            // Spell repositioning should preserve fortify.
            // So we intentionally do nothing here.
        }
    }
}