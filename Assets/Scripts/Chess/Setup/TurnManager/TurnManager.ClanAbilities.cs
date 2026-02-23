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
            _abilities = selectedClan.abilities;
            _clan = new ClanRuntime(this, board, playerTeam, queenLeader, selectedClan);

            foreach (var a in _abilities)
                if (a is IronMarch_QueenAura aura)
                    _ironMarchAura = aura;
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

        // ✅ NEW: forward OnAttackResolved → abilities
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