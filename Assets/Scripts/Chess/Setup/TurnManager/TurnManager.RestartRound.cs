using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        // =========================
        //  RESTART ROUND (Start-of-turn rewind)
        // =========================

        class TurnStartSnapshot
        {
            public int ap;
            public bool queenMovedThisTurn;
            public HashSet<Piece> movedThisTurn = new HashSet<Piece>();
            public List<PieceState> pieces = new List<PieceState>();
        }

        struct PieceState
        {
            public Piece piece;
            public Vector2Int coord;
            public int hp;
            public int fortify;
            public bool pawnHasMoved;
        }

        TurnStartSnapshot _turnStartSnapshot;

        /// <summary>
        /// Call this once at the START of the player turn (end of BeginPlayerTurn()).
        /// </summary>
        void CaptureTurnStartSnapshot()
        {
            if (board == null) return;

            _turnStartSnapshot = new TurnStartSnapshot();
            _turnStartSnapshot.ap = CurrentAP;
            _turnStartSnapshot.queenMovedThisTurn = _queenMovedThisTurn;
            _turnStartSnapshot.movedThisTurn = new HashSet<Piece>(_movedThisPlayerTurn);

            _turnStartSnapshot.pieces.Clear();

            // Snapshot every current piece on the board
            foreach (var p in board.GetAllPieces())
            {
                if (p == null) continue;

                var s = new PieceState
                {
                    piece = p,
                    coord = p.Coord,
                    hp = p.currentHP,
                    fortify = p.fortifyStacks,
                    pawnHasMoved = (p is Pawn pw) && pw.hasMoved
                };

                _turnStartSnapshot.pieces.Add(s);
            }
        }

        public void UndoButton()
        {
            if (Phase != TurnPhase.PlayerTurn) return;
            _history.Undo();
        }

        public void RedoButton()
        {
            if (Phase != TurnPhase.PlayerTurn) return;
            _history.Redo();
        }




    }
}
