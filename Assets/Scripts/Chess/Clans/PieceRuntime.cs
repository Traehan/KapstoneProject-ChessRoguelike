// Assets/Scripts/Chess/Abilities/PieceRuntime.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Lives on each spawned piece instance. Holds stats, upgrades, and dispatches hooks to abilities.
    /// </summary>
    [DisallowMultipleComponent]
    public class PieceRuntime : MonoBehaviour
    {
        // Core refs
        public Piece Owner { get; private set; }
        public ChessBoard Board { get; private set; }
        public TurnManager TM { get; private set; }

        // Runtime stats (start from Piece / PieceDefinition; can be modified by upgrades)
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public int Attack { get; set; }
        
        public int Movement { get; set; }
        
        public int CurrentAttack { get; set; }

        // Slots & lists
        [SerializeField, Min(1)] int slotsMax = 2;
        public int SlotsMax => slotsMax;
        public int SlotsUsed => upgrades.Count;
        
        public System.Collections.Generic.IReadOnlyList<PieceUpgradeSO> Upgrades => upgrades;

        readonly List<PieceAbilitySO> innate = new();
        readonly List<PieceUpgradeSO> upgrades = new();
        readonly List<PieceAbilitySO> keywordAbilities = new(); // abilities coming from upgrades
        
        bool initialized;

        // ---- Initialization ----
        public void Init(Piece owner, ChessBoard board, TurnManager tm)
        {
            if (initialized) return;
            initialized = true;

            Owner = owner; Board = board; TM = tm;

            // Seed base stats from the piece (you can swap to Definition stats if you prefer)
            MaxHP = owner.maxHP;
            CurrentHP = owner.currentHP;
            Attack = owner.attack;
            Movement = owner.Definition != null ? owner.Definition.maxStride : 1;

            // Load prefab loadout, if present
            if (owner.TryGetComponent<PieceLoadout>(out var loadout))
            {
                slotsMax = Mathf.Max(1, loadout.defaultUpgradeSlots);
                if (loadout.innateAbilities != null) innate.AddRange(loadout.innateAbilities);
            }

            // 🔹 Apply any upgrades that were purchased in the shop for THIS piece definition
            var def = owner.Definition;
            var queued = GameSession.I?.ConsumeUpgradesFor(def);
            if (queued != null)
            {
                foreach (var u in queued)
                    TryApplyUpgrade(u);  // applies stats + registers keyword ability (+OnSpawn)
            }
            
            // After applying upgrades, sync runtime stats back to the Piece so combat uses them
            if (Owner != null)
            {
                Owner.maxHP     = MaxHP;
                Owner.currentHP = CurrentHP;
                Owner.attack    = Attack;
            }


            // Fire OnSpawn for innate abilities (keyword abilities already received OnSpawn above)
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a.OnSpawn(ctx);
        }

        // ---- Upgrades ----
        public bool CanApplyUpgrade(PieceUpgradeSO u) => u != null && SlotsUsed < SlotsMax;

        public bool TryApplyUpgrade(PieceUpgradeSO u)
        {
            if (!CanApplyUpgrade(u)) return false;

            upgrades.Add(u);
            u.ApplyTo(this);  // adjust MaxHP / CurrentHP / Attack on the runtime

            // 🔹 Mirror upgraded stats back to the underlying Piece so combat uses them.
            if (Owner != null)
            {
                Owner.maxHP     = MaxHP;
                Owner.currentHP = CurrentHP;
                Owner.attack    = Attack;
            }

            // If the upgrade is also a keyword/behavior (inherits PieceAbilitySO), register it
            if (u.keywordAbility != null && !keywordAbilities.Contains(u.keywordAbility))
            {
                keywordAbilities.Add(u.keywordAbility);
                u.keywordAbility.OnSpawn(new PieceAbilitySO.PieceCtx(Owner, Board, TM));
            }

            return true;
        }


        // ---- Hook relays (to be called by TurnManager/Placement later) ----
        public void Notify_BeginPlayerTurn()
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a.OnBeginPlayerTurn(ctx);
            foreach (var a in keywordAbilities) a.OnBeginPlayerTurn(ctx);
        }

        public void Notify_EndPlayerTurn()
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a.OnEndPlayerTurn(ctx);
            foreach (var a in keywordAbilities) a.OnEndPlayerTurn(ctx);
        }

        public void Notify_PieceMoved(Vector2Int from, Vector2Int to)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a.OnPieceMoved(ctx, from, to);
            foreach (var a in keywordAbilities) a.OnPieceMoved(ctx, from, to);
        }

        public void Notify_Undo()
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a.OnUndo(ctx);
            foreach (var a in keywordAbilities) a.OnUndo(ctx);
        }
        
        // Add inside PieceRuntime class

        public int GetDisplayedAttack()
        {
            if (Owner == null) return Attack;

            int atk = Owner.attack;

            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);

            // Innate abilities
            foreach (var a in innate)
                if (a is IDisplayStatModifier mod)
                    atk += mod.GetDisplayedAttackBonus(ctx);

            // Keyword abilities from upgrades
            foreach (var a in keywordAbilities)
                if (a is IDisplayStatModifier mod)
                    atk += mod.GetDisplayedAttackBonus(ctx);

            return atk;
        }


        /// <summary>
        /// Collect pre-damage modifiers from both innate and keyword abilities.
        /// Called by ResolveCombat (we’ll wire this in a later step).
        /// </summary>
        public void CollectPreAttackModifiers(PieceAbilitySO.AttackCtx atk)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a.OnAttackPreCalc(ctx, atk);
            foreach (var a in keywordAbilities) a.OnAttackPreCalc(ctx, atk);
        }

        /// <summary>Post-damage callbacks.</summary>
        public void Notify_AttackResolved(PieceAbilitySO.AttackCtx atk)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a.OnAttackResolved(ctx, atk);
            foreach (var a in keywordAbilities) a.OnAttackResolved(ctx, atk);
        }

        // ---- Optional: hints per piece ability (aura tiles, etc.) ----
        public void ContributeHintTiles(List<(Vector2Int, Color)> outTiles)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            var tmp = new List<Vector2Int>();
            if (innate != null)
            {
                foreach (var a in innate)
                {
                    tmp.Clear();
                    if (a.TryGetHintTiles(ctx, tmp, out var color))
                        foreach (var t in tmp) outTiles.Add((t, color));
                }
            }
            if (keywordAbilities != null)
            {
                foreach (var a in keywordAbilities)
                {
                    tmp.Clear();
                    if (a.TryGetHintTiles(ctx, tmp, out var color))
                        foreach (var t in tmp) outTiles.Add((t, color));
                }
            }
        }
        
        // ---- UI Hooks ----
        // Called by Unity when the user clicks this piece's collider in the scene.
        void OnMouseDown()
        {
            // Make sure we have everything we need
            if (Owner == null || Board == null || TM == null) return;

            // Only react during encounters (ignore prep if you want)
            // If you want it to also work during Preparation, remove this check.
            if (TM.Phase != TurnPhase.PlayerTurn && TM.Phase != TurnPhase.EnemyTurn)
                return;

            // Only show info for the player's pieces
            // if (Owner.Team != TM.PlayerTeam)
            //     return;

            // Ask the global UI panel to show info for this piece
            if (PieceInfoPanel.Instance != null)
            {
                PieceInfoPanel.Instance.Show(this);
            }
        }

    }
}
