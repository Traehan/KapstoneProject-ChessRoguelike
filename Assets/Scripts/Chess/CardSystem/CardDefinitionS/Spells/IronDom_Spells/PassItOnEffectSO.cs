using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Pass It On", fileName = "PassItOnEffect")]
    public class PassItOnEffectSO : SpellEffectSO
    {
        [Min(1)] public int destinationCap = 20;

        const string SourceKey = "PassItOn_Source";
        const string DestKey = "PassItOn_Dest";
        const string SourcePrevKey = "PassItOn_SourcePrev";
        const string DestPrevKey = "PassItOn_DestPrev";

        public override bool Resolve(SpellContext context)
        {
            if (context == null)
                return false;

            if (!(context.Target is TransferFortifyTarget target))
                return false;

            if (target.source == null || target.destination == null)
                return false;

            if (target.source == target.destination)
                return false;

            if (target.source.Team != context.CasterTeam || target.destination.Team != context.CasterTeam)
                return false;

            int sourceFortify = FortifyStatusUtility.GetFortify(target.source);
            int destFortify = FortifyStatusUtility.GetFortify(target.destination);

            if (sourceFortify <= 0)
                return false;

            int newDest = Mathf.Min(destFortify + sourceFortify, destinationCap);

            context.SetState(SourceKey, target.source);
            context.SetState(DestKey, target.destination);
            context.SetState(SourcePrevKey, sourceFortify);
            context.SetState(DestPrevKey, destFortify);

            FortifyStatusUtility.SetFortify(target.source, 0);
            FortifyStatusUtility.SetFortify(target.destination, newDest);

            GameEvents.OnPieceStatsChanged?.Invoke(target.source);
            GameEvents.OnPieceStatsChanged?.Invoke(target.destination);
            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null)
                return;

            if (!context.TryGetState<Piece>(SourceKey, out var source) || source == null)
                return;
            if (!context.TryGetState<Piece>(DestKey, out var dest) || dest == null)
                return;
            if (!context.TryGetState<int>(SourcePrevKey, out var sourcePrev))
                return;
            if (!context.TryGetState<int>(DestPrevKey, out var destPrev))
                return;

            FortifyStatusUtility.SetFortify(source, sourcePrev);
            FortifyStatusUtility.SetFortify(dest, destPrev);

            GameEvents.OnPieceStatsChanged?.Invoke(source);
            GameEvents.OnPieceStatsChanged?.Invoke(dest);
        }
    }
}