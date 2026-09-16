using UnityEngine;

namespace Chess
{
    // The first time one of your pieces gains Fortify each battle, that gain is doubled.
    [CreateAssetMenu(menuName = "Relics/Iron Dominion/First Fortify Doubles")]
    public class FirstFortifyDoublesRelicSO : RelicSO
    {
        bool _consumedThisBattle;

        public override void OnClanEquipped(ClanRuntime ctx)
        {
            _consumedThisBattle = false;
        }

        public override void OnStatusApplied(ClanRuntime ctx, StatusChangeReport report)
        {
            if (_consumedThisBattle) return;
            if (report.statusId != StatusId.Fortify) return;
            if (report.amountChanged <= 0) return;
            if (report.piece == null || ctx == null || report.piece.Team != ctx.playerTeam) return;

            // Set the guard before granting more Fortify: AddFortify raises OnStatusApplied again,
            // and without this the doubling would re-trigger itself recursively.
            _consumedThisBattle = true;
            FortifyStatusUtility.AddFortify(report.piece, report.amountChanged, int.MaxValue, report.source);
        }
    }
}
