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

        public static void AddRetaliate(Piece piece, int amount, Piece source = null)
        {
            if (piece == null || amount <= 0) return;
            var sc = GetOrAddStatusController(piece);
            sc.AddStacks(StatusId.Retaliate, amount, source);
        }

        public static void SetRetaliate(Piece piece, int stacks, Piece source = null)
        {
            if (piece == null) return;
            var sc = GetOrAddStatusController(piece);
            sc.SetStacks(StatusId.Retaliate, Mathf.Max(0, stacks), source);
        }

        public static void RemoveRetaliate(Piece piece, int amount, Piece source = null)
        {
            if (piece == null || amount <= 0) return;

            int current = GetRetaliate(piece);
            SetRetaliate(piece, Mathf.Max(0, current - amount), source);
        }

        public static void ClearRetaliate(Piece piece, Piece source = null)
        {
            if (piece == null) return;
            var sc = piece.GetComponent<StatusController>();
            if (sc == null) return;
            sc.Clear(StatusId.Retaliate, source);
        }
    }
}