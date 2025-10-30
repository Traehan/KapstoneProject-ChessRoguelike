using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public abstract class AbilitySO : ScriptableObject
    {
        [TextArea] public string description;

        // Lifecycle (called by TurnManager)
        public virtual void OnClanEquipped(ClanRuntime ctx) {}
        public virtual void OnBeginPlayerTurn(ClanRuntime ctx) {}
        public virtual void OnEndPlayerTurn(ClanRuntime ctx) {}
        public virtual void OnPieceMoved(ClanRuntime ctx, Piece piece) {}
        public virtual void OnAttackResolved(ClanRuntime ctx, Piece attacker, Piece defender,
            int dmgToDef, int dmgToAtk) {}
        public virtual void OnUndo(ClanRuntime ctx, object undoPayload) {}

        // Optional board hints (e.g., Queen aura). Return tiles to tint + color.
        public virtual bool TryGetHintTiles(ClanRuntime ctx, out List<Vector2Int> tiles, out Color color)
        { tiles = null; color = default; return false; }
    }
}