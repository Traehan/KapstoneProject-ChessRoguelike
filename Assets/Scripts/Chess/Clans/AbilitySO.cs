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
        public virtual void OnPieceCaptured(ClanRuntime ctx, Piece victim, Piece by, Vector2Int at) {}
        public virtual void OnUndo(ClanRuntime ctx, object undoPayload) {}

        // Fired whenever any status (Fortify, Bleed, Retaliate, ...) is applied to any piece this battle.
        public virtual void OnStatusApplied(ClanRuntime ctx, StatusChangeReport report) {}

        // Lets an ability/relic add flat bonus damage to a Retaliate counter-hit. Summed across all abilities.
        public virtual int GetRetaliateBonusDamage(ClanRuntime ctx, Piece retaliator, Piece attacker) => 0;

        // Optional board hints (e.g., Queen aura). Return tiles to tint + color.
        public virtual bool TryGetHintTiles(ClanRuntime ctx, out List<Vector2Int> tiles, out Color color)
        { tiles = null; color = default; return false; }
    }
}