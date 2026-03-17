using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Blood Court/Apply Bleed Scaling", fileName = "FX_ApplyBleedScaling")]
    public class ApplyBleedScalingEffectSO : SpellEffectSO
    {
        [Header("Bleed Values")]
        [Min(0)] public int baseBleed = 2;
        [Min(0)] public int bonusIfAlreadyBleeding = 2;

        Piece _target;
        StatusController _status;
        int _appliedAmount;

        public override bool Resolve(SpellContext context)
        {
            if (context == null)
                return false;

            _target = context.Target as Piece;
            if (_target == null)
                return false;

            _status = _target.GetComponent<StatusController>();
            if (_status == null)
                return false;

            int currentBleed = _status.GetStacks(StatusId.Bleed);
            _appliedAmount = baseBleed;

            if (currentBleed > 0)
                _appliedAmount += bonusIfAlreadyBleeding;

            if (_appliedAmount <= 0)
                return true;

            _status.AddStacks(StatusId.Bleed, _appliedAmount);
            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (_status == null || _appliedAmount <= 0)
                return;

            int current = _status.GetStacks(StatusId.Bleed);
            int newValue = Mathf.Max(0, current - _appliedAmount);
            _status.SetStacks(StatusId.Bleed, newValue);
        }
    }
}