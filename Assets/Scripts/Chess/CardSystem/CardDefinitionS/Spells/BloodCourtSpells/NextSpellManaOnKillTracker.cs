using UnityEngine;
using Chess;

namespace Card
{
    public sealed class NextSpellManaOnKillTracker : MonoBehaviour
    {
        Piece _owner;
        PieceRuntime _runtime;
        TurnManager _tm;

        int _attackBonus;
        int _nextSpellManaBonus;

        bool _sawPlayerTurn;
        bool _manaGranted;
        bool _cleanedUp;
        bool _subscribed;

        public void Init(Piece owner, TurnManager tm, int attackBonus, int nextSpellManaBonus)
        {
            _owner = owner;
            _runtime = owner != null ? owner.GetComponent<PieceRuntime>() : null;
            _tm = tm;
            _attackBonus = attackBonus;
            _nextSpellManaBonus = nextSpellManaBonus;

            ApplyAttackBonus();
            Subscribe();
        }

        void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            GameEvents.OnAttackResolved += HandleAttackResolved;
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            GameEvents.OnAttackResolved -= HandleAttackResolved;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        void HandlePhaseChanged(TurnPhase phase)
        {
            if (_cleanedUp) return;

            if (phase == TurnPhase.PlayerTurn)
            {
                _sawPlayerTurn = true;
                return;
            }

            // Once the player's battle phase is over, remove the temporary buff.
            if (_sawPlayerTurn && phase != TurnPhase.PlayerTurn)
                Cleanup();
        }

        void HandleAttackResolved(AttackReport report)
        {
            if (_cleanedUp || _manaGranted || !_sawPlayerTurn)
                return;

            if (_owner == null || _tm == null)
                return;

            if (report.attacker != _owner)
                return;

            if (!report.defenderDied || report.defender == null)
                return;

            if (report.defender.Team == _owner.Team)
                return;

            _tm.GrantNextSpellPhaseMana(_nextSpellManaBonus);
            _manaGranted = true;
        }

        void ApplyAttackBonus()
        {
            if (_owner == null) return;

            if (_runtime != null)
            {
                _runtime.Attack += _attackBonus;
                _owner.attack = _runtime.Attack;
            }
            else
            {
                _owner.attack += _attackBonus;
            }

            GameEvents.OnPieceStatsChanged?.Invoke(_owner);
        }

        void RemoveAttackBonus()
        {
            if (_owner == null) return;

            if (_runtime != null)
            {
                _runtime.Attack -= _attackBonus;
                _owner.attack = _runtime.Attack;
            }
            else
            {
                _owner.attack -= _attackBonus;
            }

            GameEvents.OnPieceStatsChanged?.Invoke(_owner);
        }

        public void UndoTracker()
        {
            if (_manaGranted && _tm != null)
                _tm.RemovePendingNextSpellPhaseMana(_nextSpellManaBonus);

            Cleanup();
        }

        void Cleanup()
        {
            if (_cleanedUp) return;
            _cleanedUp = true;

            RemoveAttackBonus();
            Unsubscribe();
            Destroy(this);
        }
    }
}