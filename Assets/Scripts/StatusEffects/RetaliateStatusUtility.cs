using UnityEngine;

namespace Chess
{
    public static class RetaliateStatusUtility
    {
        public static StatusController GetOrAddStatusController(Piece piece)
        {
            if (piece == null) return null;
            return piece.GetComponent<StatusController>() ??
                   piece.gameObject.AddComponent<StatusController>();
        }

        public static int GetRetaliate(Piece piece)
        {
            if (piece == null) return 0;
            var sc = piece.GetComponent<StatusController>();
            return sc != null ? sc.GetStacks(StatusId.Retaliate) : 0;
        }

        public static bool HasRetaliate(Piece piece)
        {
            return GetRetaliate(piece) > 0;
        }

        public static void AddRetaliate(Piece piece, int amount)
        {
            if (piece == null || amount <= 0) return;
            var sc = GetOrAddStatusController(piece);
            sc.AddStacks(StatusId.Retaliate, amount);
        }

        public static void SetRetaliate(Piece piece, int stacks)
        {
            if (piece == null) return;
            var sc = GetOrAddStatusController(piece);
            sc.SetStacks(StatusId.Retaliate, Mathf.Max(0, stacks));
        }

        public static void RemoveRetaliate(Piece piece, int amount)
        {
            if (piece == null || amount <= 0) return;

            int current = GetRetaliate(piece);
            SetRetaliate(piece, Mathf.Max(0, current - amount));
        }

        public static void ClearRetaliate(Piece piece)
        {
            if (piece == null) return;
            var sc = piece.GetComponent<StatusController>();
            if (sc == null) return;
            sc.Clear(StatusId.Retaliate);
        }
    }
}