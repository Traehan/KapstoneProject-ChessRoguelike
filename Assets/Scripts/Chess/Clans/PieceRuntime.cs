using System.Collections.Generic;
using UnityEngine;
using Card;

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
        public IReadOnlyList<PieceUpgradeSO> Upgrades => upgrades;

        readonly List<PieceAbilitySO> innate = new();
        readonly List<PieceUpgradeSO> upgrades = new();
        readonly List<PieceAbilitySO> keywordAbilities = new();

        bool initialized;
        bool inspecting;

        void Update()
        {
            if (Input.GetMouseButton(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider != null && hit.collider.gameObject == gameObject)
                    {
                        if (!inspecting)
                        {
                            inspecting = true;
                            if (PieceInfoPanel.Instance != null)
                                PieceInfoPanel.Instance.Show(this);
                        }

                        return;
                    }
                }
            }

            if (inspecting && Input.GetMouseButtonUp(1))
            {
                inspecting = false;
                if (PieceInfoPanel.Instance != null)
                    PieceInfoPanel.Instance.Hide();
            }
        }

        public void Init(Piece owner, ChessBoard board, TurnManager tm)
        {
            if (initialized) return;
            initialized = true;

            Owner = owner;
            Board = board;
            TM = tm;

            MaxHP = owner.maxHP;
            CurrentHP = owner.currentHP;
            Attack = owner.attack;
            Movement = owner.Definition != null ? owner.Definition.maxStride : 1;

            if (owner.TryGetComponent<PieceLoadout>(out var loadout))
            {
                slotsMax = Mathf.Max(1, loadout.defaultUpgradeSlots);
                if (loadout.innateAbilities != null) innate.AddRange(loadout.innateAbilities);
            }

            var def = owner.Definition;
            var queued = GameSession.I?.ConsumeUpgradesFor(def);
            if (queued != null)
            {
                foreach (var u in queued)
                    TryApplyUpgrade(u);
            }

            SyncStatsToOwner();

            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate)
                a?.OnSpawn(ctx);
        }

        public bool CanApplyUpgrade(PieceUpgradeSO u) => u != null && SlotsUsed < SlotsMax;

        public bool TryApplyUpgrade(PieceUpgradeSO u)
        {
            if (!CanApplyUpgrade(u)) return false;

            upgrades.Add(u);
            u.ApplyTo(this);
            SyncStatsToOwner();

            if (u.keywordAbility != null && !keywordAbilities.Contains(u.keywordAbility))
            {
                keywordAbilities.Add(u.keywordAbility);
                u.keywordAbility.OnSpawn(new PieceAbilitySO.PieceCtx(Owner, Board, TM));
            }

            return true;
        }

        void SyncStatsToOwner()
        {
            if (Owner == null) return;
            Owner.maxHP = MaxHP;
            Owner.currentHP = CurrentHP;
            Owner.attack = Attack;
        }

        public void Notify_BeginPlayerTurn()
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnBeginPlayerTurn(ctx);
            foreach (var a in keywordAbilities) a?.OnBeginPlayerTurn(ctx);
        }

        public void Notify_EndPlayerTurn()
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnEndPlayerTurn(ctx);
            foreach (var a in keywordAbilities) a?.OnEndPlayerTurn(ctx);
        }

        public void Notify_PieceMoved(Vector2Int from, Vector2Int to)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnPieceMoved(ctx, from, to);
            foreach (var a in keywordAbilities) a?.OnPieceMoved(ctx, from, to);
        }

        public void Notify_Undo()
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnUndo(ctx);
            foreach (var a in keywordAbilities) a?.OnUndo(ctx);
        }

        public void Notify_SpellCardPlayed(Card.Card card, SpellCardPlayReport report)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnSpellCardPlayed(ctx, card, report);
            foreach (var a in keywordAbilities) a?.OnSpellCardPlayed(ctx, card, report);
        }

        public void Notify_UnitCardPlayed(Card.Card card, UnitCardPlayReport report)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnUnitCardPlayed(ctx, card, report);
            foreach (var a in keywordAbilities) a?.OnUnitCardPlayed(ctx, card, report);
        }
        
        public void Notify_PieceCaptured(Piece victim, Piece by, Vector2Int at)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnPieceCaptured(ctx, victim, by, at);
            foreach (var a in keywordAbilities) a?.OnPieceCaptured(ctx, victim, by, at);
        }

        public int GetDisplayedAttack()
        {
            if (Owner == null) return Attack;

            int atk = Owner.attack;
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);

            foreach (var a in innate)
                if (a is IDisplayStatModifier mod)
                    atk += mod.GetDisplayedAttackBonus(ctx);

            foreach (var a in keywordAbilities)
                if (a is IDisplayStatModifier mod)
                    atk += mod.GetDisplayedAttackBonus(ctx);

            return atk;
        }

        public void CollectPreAttackModifiers(PieceAbilitySO.AttackCtx atk)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnAttackPreCalc(ctx, atk);
            foreach (var a in keywordAbilities) a?.OnAttackPreCalc(ctx, atk);
        }

        public void Notify_AttackResolved(PieceAbilitySO.AttackCtx atk)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            foreach (var a in innate) a?.OnAttackResolved(ctx, atk);
            foreach (var a in keywordAbilities) a?.OnAttackResolved(ctx, atk);
        }

        public void ContributeHintTiles(List<(Vector2Int, Color)> outTiles)
        {
            var ctx = new PieceAbilitySO.PieceCtx(Owner, Board, TM);
            var tmp = new List<Vector2Int>();

            if (innate != null)
            {
                foreach (var a in innate)
                {
                    tmp.Clear();
                    if (a != null && a.TryGetHintTiles(ctx, tmp, out var color))
                        foreach (var t in tmp) outTiles.Add((t, color));
                }
            }

            if (keywordAbilities != null)
            {
                foreach (var a in keywordAbilities)
                {
                    tmp.Clear();
                    if (a != null && a.TryGetHintTiles(ctx, tmp, out var color))
                        foreach (var t in tmp) outTiles.Add((t, color));
                }
            }
        }

        void OnMouseDown()
        {
            if (Owner == null || Board == null || TM == null) return;
            if (TM.Phase != TurnPhase.PlayerTurn && TM.Phase != TurnPhase.EnemyTurn) return;
        }
    }
}