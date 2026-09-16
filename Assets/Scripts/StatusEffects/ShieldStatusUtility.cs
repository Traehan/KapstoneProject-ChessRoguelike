using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Enemy-side counterpart to FortifyStatusUtility. Mechanically identical (stacking shield that
    /// absorbs incoming damage 1:1 before HP), but genuinely separate from Fortify: Shield is never
    /// halved by movement (see MoveCommand, which only halves StatusId.Fortify), since enemies are
    /// expected to move on nearly every turn. Nothing currently grants Shield to any enemy piece —
    /// this is just the mechanical primitive for EncounterDefinition/enemy authoring to use later.
    /// </summary>
    public static class ShieldStatusUtility
    {
        public static StatusController GetOrAddStatusController(Piece piece)
        {
            if (piece == null) return null;
            return piece.GetComponent<StatusController>() ??
                   piece.gameObject.AddComponent<StatusController>();
        }

        public static int GetShield(Piece piece)
        {
            if (piece == null) return 0;
            var sc = piece.GetComponent<StatusController>();
            return sc != null ? sc.GetStacks(StatusId.Shield) : 0;
        }

        public static void AddShield(Piece piece, int amount, int maxStacks, Piece source = null)
        {
            if (piece == null || amount <= 0 || maxStacks <= 0) return;

            var sc = GetOrAddStatusController(piece);
            int current = sc.GetStacks(StatusId.Shield);
            if (current >= maxStacks) return;

            int addAmount = Mathf.Min(amount, maxStacks - current);
            sc.AddStacks(StatusId.Shield, addAmount, source);

            Debug.Log($"[Shield] {piece.name}: {current} -> {sc.GetStacks(StatusId.Shield)}");
        }

        public static void SetShield(Piece piece, int stacks, Piece source = null)
        {
            if (piece == null) return;
            var sc = GetOrAddStatusController(piece);
            sc.SetStacks(StatusId.Shield, Mathf.Max(0, stacks), source);
        }

        public static void ClearShield(Piece piece, Piece source = null)
        {
            if (piece == null) return;
            var sc = piece.GetComponent<StatusController>();
            if (sc == null) return;
            sc.Clear(StatusId.Shield, source);
        }

        public static void RemoveShield(Piece piece, int amount, Piece source = null)
        {
            if (piece == null || amount <= 0) return;

            int current = GetShield(piece);
            SetShield(piece, Mathf.Max(0, current - amount), source);
        }

        public static int AbsorbDamage(Piece piece, int incomingDamage, bool bypassShield = false, Piece source = null)
        {
            if (piece == null) return Mathf.Max(0, incomingDamage);
            if (incomingDamage <= 0) return 0;
            if (bypassShield) return incomingDamage;

            int shield = GetShield(piece);
            if (shield <= 0) return incomingDamage;

            int absorbed = Mathf.Min(shield, incomingDamage);
            int remainingShield = shield - absorbed;
            int remainingDamage = incomingDamage - absorbed;

            SetShield(piece, remainingShield, source);
            return remainingDamage;
        }
    }
}
