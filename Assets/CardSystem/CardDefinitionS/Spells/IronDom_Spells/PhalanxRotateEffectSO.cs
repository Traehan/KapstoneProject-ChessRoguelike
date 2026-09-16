using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Phalanx Rotate", fileName = "PhalanxRotateEffect")]
    public class PhalanxRotateEffectSO : SpellEffectSO
    {
        const string FirstKey = "PhalanxRotate_First";
        const string SecondKey = "PhalanxRotate_Second";
        const string FirstCoordKey = "PhalanxRotate_FirstCoord";
        const string SecondCoordKey = "PhalanxRotate_SecondCoord";

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.Board == null)
                return false;

            if (!(context.Target is PhalanxRotateTarget target))
                return false;

            if (target.first == null || target.second == null)
                return false;

            if (target.first == target.second)
                return false;

            if (target.first.Team != context.CasterTeam)
                return false;

            Vector2Int a = target.first.Coord;
            Vector2Int b = target.second.Coord;

            int manhattan = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            if (manhattan != 1)
                return false;

            context.SetState(FirstKey, target.first);
            context.SetState(SecondKey, target.second);
            context.SetState(FirstCoordKey, a);
            context.SetState(SecondCoordKey, b);

            // swap using raw board placement
            bool swapped = context.Board.SwapPiecesWithoutCapture(target.first, target.second);
            if (!swapped)
                return false;

            target.first.GetComponent<PieceRuntime>()?.Notify_PieceMoved(a, b);
            target.second.GetComponent<PieceRuntime>()?.Notify_PieceMoved(b, a);

            GameEvents.OnPieceMoved?.Invoke(target.first, a, b, MoveReason.SpellEffect);
            GameEvents.OnPieceMoved?.Invoke(target.second, b, a, MoveReason.SpellEffect);
            GameEvents.OnPieceStatsChanged?.Invoke(target.first);
            GameEvents.OnPieceStatsChanged?.Invoke(target.second);

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null || context.Board == null)
                return;

            if (!context.TryGetState<Piece>(FirstKey, out var first) || first == null)
                return;
            if (!context.TryGetState<Piece>(SecondKey, out var second) || second == null)
                return;
            if (!context.TryGetState<Vector2Int>(FirstCoordKey, out var firstCoord))
                return;
            if (!context.TryGetState<Vector2Int>(SecondCoordKey, out var secondCoord))
                return;

            bool restored = context.Board.SwapPiecesWithoutCapture(first, second);
            if (!restored)
                return;

            first.GetComponent<PieceRuntime>()?.Notify_Undo();
            second.GetComponent<PieceRuntime>()?.Notify_Undo();

            GameEvents.OnPieceMoved?.Invoke(first, secondCoord, firstCoord, MoveReason.Undo);
            GameEvents.OnPieceMoved?.Invoke(second, firstCoord, secondCoord, MoveReason.Undo);
            GameEvents.OnPieceStatsChanged?.Invoke(first);
            GameEvents.OnPieceStatsChanged?.Invoke(second);
        }
    }
}