using System.Collections.Generic;
using UnityEngine;

namespace ChessRL
{
    [RequireComponent(typeof(Collider))]
    public abstract class Piece : MonoBehaviour
    {
        public Team Team { get; private set; }
        public Vector2Int Coord { get; private set; }
        public ChessBoard Board { get; private set; }

        // Optional combat-ish stats you can grow later (HP, ATK, etc.)
        [Header("Stats (expand later)")]
        public int maxHP = 1;
        public int currentHP = 1;
        public int attack = 1;

        public virtual void Init(ChessBoard board, Team team, Vector2Int start)
        {
            Board = board;
            Team  = team;
            SetCoord(start, snap:true);
            name = $"{team} {GetType().Name} {TileName(start)}";
        }

        protected void SetCoord(Vector2Int c, bool snap)
        {
            Coord = c;
            if (snap && Board != null)
                transform.position = Board.BoardToWorldCenter(c);
        }

        public virtual void OnPlaced() { }
        public virtual void OnCaptured() { Destroy(gameObject); }

        // === Movement API ===
        public abstract void GetLegalMoves(List<Vector2Int> buffer);

        // Helper to push move if inside board and empty
        protected bool PushIfEmpty(Vector2Int c, List<Vector2Int> buffer)
        {
            if (!Board.InBounds(c)) return false;
            if (!Board.IsOccupied(c)) { buffer.Add(c); return true; }
            return false;
        }

        // Helper to push capture if enemy there
        protected bool PushIfEnemy(Vector2Int c, List<Vector2Int> buffer)
        {
            if (!Board.InBounds(c)) return false;
            var p = Board.GetPiece(c);
            if (p != null && p.Team != Team) { buffer.Add(c); return true; }
            return false;
        }

        protected static string TileName(Vector2Int c)
        {
            string col = "";
            int x = c.x;
            do { col = (char)('A' + (x % 26)) + col; x = x / 26 - 1; } while (x >= 0);
            return $"{col}{c.y + 1}";
        }
    }
}

