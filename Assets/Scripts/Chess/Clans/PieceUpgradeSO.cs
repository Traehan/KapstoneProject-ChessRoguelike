// Assets/Scripts/Chess/Abilities/PieceUpgradeSO.cs
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Simple, stackable stat/keyword upgrades for a single piece instance.
    /// For keyword/rule behavior, inherit from PieceAbilitySO instead.
    /// </summary>
    [CreateAssetMenu(menuName = "Upgrades", fileName = "PieceUpgrades")]
    public class PieceUpgradeSO : ScriptableObject
    {
        [Header("Meta")]
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Stat Deltas")]
        public int addMaxHP;
        public int addAttack;

        [Header("Optional Keyword/Behavior")]
        public PieceAbilitySO keywordAbility;   // leave null for pure stat upgrades

        [Header("Shop")]
        public int shopPrice = 25;

        /// <summary>
        /// Apply stat deltas to the piece. Called once when the upgrade is bought/equipped.
        /// Keep it side-effect free (no turn hooks here).
        /// </summary>
        public virtual void ApplyTo(PieceRuntime runtime)
        {
            if (addMaxHP != 0)
            {
                runtime.MaxHP += addMaxHP;
                runtime.CurrentHP = Mathf.Min(runtime.CurrentHP + addMaxHP, runtime.MaxHP);
            }
            if (addAttack != 0)
            {
                runtime.Attack += addAttack;
            }
            // keywordAbility registration is handled by PieceRuntime.TryApplyUpgrade
        }
    }
}