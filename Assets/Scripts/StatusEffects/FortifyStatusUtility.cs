using UnityEngine;

namespace Chess
{
    public static class FortifyStatusUtility
    {
        public static StatusController GetOrAddStatusController(Piece piece)
        {
            if (piece == null) return null;
            return piece.GetComponent<StatusController>() ??
                   piece.gameObject.AddComponent<StatusController>();
        }

        public static int GetFortify(Piece piece)
        {
            if (piece == null) return 0;
            var sc = piece.GetComponent<StatusController>();
            return sc != null ? sc.GetStacks(StatusId.Fortify) : 0;
        }

        public static void AddFortify(Piece piece, int amount, int maxStacks, Piece source = null)
        {
            if (piece == null || amount <= 0 || maxStacks <= 0) return;

            var sc = GetOrAddStatusController(piece);
            int current = sc.GetStacks(StatusId.Fortify);
            if (current >= maxStacks) return;

            int addAmount = Mathf.Min(amount, maxStacks - current);
            sc.AddStacks(StatusId.Fortify, addAmount, source);

            Debug.Log($"[Fortify] {piece.name}: {current} -> {sc.GetStacks(StatusId.Fortify)}");
        }

        public static void SetFortify(Piece piece, int stacks, Piece source = null)
        {
            if (piece == null) return;
            var sc = GetOrAddStatusController(piece);
            sc.SetStacks(StatusId.Fortify, Mathf.Max(0, stacks), source);
        }

        public static void ClearFortify(Piece piece, Piece source = null)
        {
            if (piece == null) return;
            var sc = piece.GetComponent<StatusController>();
            if (sc == null) return;
            sc.Clear(StatusId.Fortify, source);
        }

        public static void RemoveFortify(Piece piece, int amount, Piece source = null)
        {
            if (piece == null || amount <= 0) return;

            int current = GetFortify(piece);
            SetFortify(piece, Mathf.Max(0, current - amount), source);
        }

        public static int AbsorbDamage(Piece piece, int incomingDamage, bool bypassFortify = false, Piece source = null)
        {
            if (piece == null) return Mathf.Max(0, incomingDamage);
            if (incomingDamage <= 0) return 0;
            if (bypassFortify) return incomingDamage;

            int fortify = GetFortify(piece);
            if (fortify <= 0) return incomingDamage;

            int absorbed = Mathf.Min(fortify, incomingDamage);
            int remainingFortify = fortify - absorbed;
            int remainingDamage = incomingDamage - absorbed;

            SetFortify(piece, remainingFortify, source);
            return remainingDamage;
        }
    }
}