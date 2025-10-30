using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Collider))]
    public abstract class Piece : MonoBehaviour
    {
        public Team Team { get; private set; }
        public Vector2Int Coord { get; private set; }
        public ChessBoard Board { get; private set; }

        [Header("Data")]
        [SerializeField] private PieceDefinition definition;
        public PieceDefinition Definition => definition;  // read-only accessor

        // Keep these for backwards compatibility (serialized on old prefabs)
        [Header("Stats (runtime)")]
        [HideInInspector]public int maxHP = 1;
        [HideInInspector]public int currentHP = 1;
        [HideInInspector]public int attack = 1;
        
        //----FOR CLANS----//
        [Header("Combat Runtime")]
        [Tooltip("Iron March: reduces incoming damage by this amount; stacks on end-turn if stationary.")]
        public int fortifyStacks = 0;


        public virtual void Init(ChessBoard board, Team team, Vector2Int start)
        {
            Board = board;
            Team  = team;
            SetCoord(start, snap:true);
            name = $"{team} {GetType().Name} {TileName(start)}";

            // NEW: hydrate stats from ScriptableObject (if assigned)
            if (definition)
            {
                maxHP     = definition.maxHP;
                currentHP = definition.maxHP;
                attack    = definition.attack;
            }
        }
        
        public void EnsureDefinition(PieceDefinition def)
        {
            if (def == null) return;
            if (definition != null) return;

            definition = def;

            // If Init already ran, sync runtime stats now.
            if (Board != null)
            {
                maxHP     = definition.maxHP;
                currentHP = definition.maxHP;
                attack    = definition.attack;
            }
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
        
        // Public entry point the board can call to move a piece safely.
        public void ApplyBoardMove(Vector2Int to, bool snap = true)
        {
            SetCoord(to, snap);
            OnAfterBoardMove();
        }

        /// <summary>
        /// Hook for subclasses (e.g., Pawn) to react to a successful board move
        /// without the board needing to know about specific piece types.
        /// </summary>
        protected virtual void OnAfterBoardMove() { }


        protected static string TileName(Vector2Int c)
        {
            string col = "";
            int x = c.x;
            do { col = (char)('A' + (x % 26)) + col; x = x / 26 - 1; } while (x >= 0);
            return $"{col}{c.y + 1}";
        }
        
        //-----IRON MARCH----//
        public void AddFortify(int maxStacks = 3)
        {
            fortifyStacks = Mathf.Clamp(fortifyStacks + 1, 0, maxStacks);
        }

        public void ClearFortify()
        {
            fortifyStacks = 0;
        }
        
#if UNITY_EDITOR
        void OnValidate()
        {
            if (definition != null)
            {
                maxHP = definition.maxHP;
                currentHP = definition.maxHP;
                attack = definition.attack;
            }
        }
#endif
    }
    
}

