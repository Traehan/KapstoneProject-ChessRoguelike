using System.Linq;
using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        public void PaintAbilityHints()
        {
            if (_abilities == null) return;
            foreach (var a in _abilities)
                if (a != null && a.TryGetHintTiles(_clan, out var t, out var c))
                    board.Highlight(t, c);
        }

        void EnsureQueenLeaderBound()
        {
            queenLeader ??= board.GetAllPieces().OfType<Queen>()
                .FirstOrDefault(q => q.Team == playerTeam);

            if (_clan != null) _clan.queen = queenLeader;
        }

        void BuildClanRuntime()
        {
            if (selectedClan == null) return;

            var relics = GameSession.I != null ? GameSession.I.ownedRelics : null;
            _abilities = (relics != null && relics.Count > 0)
                ? selectedClan.abilities.Concat(relics.Cast<AbilitySO>()).ToArray()
                : selectedClan.abilities;

            _clan = new ClanRuntime(this, board, playerTeam, queenLeader, selectedClan);

            foreach (var a in _abilities)
                if (a is IronMarch_QueenAura aura)
                    _ironMarchAura = aura;

            foreach (var a in _abilities)
                a?.OnClanEquipped(_clan);
        }

        // Forwards GameEvents.OnStatusApplied (any status, any piece) to abilities/relics.
        public void NotifyAbilitiesStatusApplied(StatusChangeReport report)
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnStatusApplied(_clan, report);
        }

        // Sums bonus Retaliate damage contributed by abilities/relics (e.g. Counterweight).
        public int GetRelicRetaliateBonusDamage(Piece retaliator, Piece attacker)
        {
            if (_abilities == null) return 0;

            int bonus = 0;
            foreach (var a in _abilities)
                bonus += a?.GetRetaliateBonusDamage(_clan, retaliator, attacker) ?? 0;
            return bonus;
        }

        void NotifyAbilitiesBeginPlayerTurn()
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnBeginPlayerTurn(_clan);
        }

        void NotifyAbilitiesEndPlayerTurn()
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnEndPlayerTurn(_clan);
        }

        public void NotifyAbilitiesPieceMoved(Piece p)
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnPieceMoved(_clan, p);
        }

        // NEW: forward OnAttackResolved → abilities
        void NotifyAbilitiesAttackResolved(AttackReport r)
        {
            if (_abilities == null) return;

            foreach (var a in _abilities)
            {
                a?.OnAttackResolved(_clan,
                    r.attacker,
                    r.defender,
                    r.damageToDefender,
                    r.damageToAttacker);
            }
        }
    }
}