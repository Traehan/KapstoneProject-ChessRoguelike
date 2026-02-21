// Assets/Scripts/Chess/Events/IDisplayStatModifier.cs
namespace Chess
{
    /// <summary>
    /// Optional interface for abilities/upgrades that want to change what the UI displays
    /// (without permanently changing base stats).
    /// </summary>
    public interface IDisplayStatModifier
    {
        int GetDisplayedAttackBonus(PieceAbilitySO.PieceCtx ctx);
        // Later you can add: GetDisplayedMaxHPBonus, GetDisplayedArmorBonus, etc.
    }
}