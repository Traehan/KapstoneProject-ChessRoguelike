// Assets/Scripts/Chess/Abilities/PhaseUpgradeSO.cs
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Upgrades/Phase (Pass Through Allies)",
        fileName = "UPG_Phase")]
    public class PhaseUpgradeSO : PieceUpgradeSO
    {
        public override void ApplyTo(PieceRuntime runtime)
        {
            // Keep normal stat behavior if you set addMaxHP/addAttack
            base.ApplyTo(runtime);

            if (runtime == null || runtime.Owner == null) return;

            var def = runtime.Owner.Definition;
            if (def != null)
            {
                def.passThroughFriendlies = true;
#if UNITY_EDITOR
                Debug.Log($"[PhaseUpgradeSO] {def.displayName} now passes through allies.");
#endif
            }
        }
    }
}