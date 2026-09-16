// Gauntlet (Elite/Trial) risk-reward system: independently-rolled reward and challenge types for
// a map Encounter node flagged as a Gauntlet. Lives in the global namespace to match the sibling
// MapNodeType/MapMovementType enums it's authored alongside.

/// <summary>Reward rolled for a Gauntlet node, granted on victory only if the player accepted.</summary>
public enum GauntletRewardType
{
    Gold,
    QueenMove,
    Relic
}

/// <summary>Challenge modifier rolled for a Gauntlet node, applied to the fight only if accepted.</summary>
public enum GauntletChallengeType
{
    StatBoost,
    Swarm,
    HarderEnemy,
    ManaHandicap,
    EnergyHandicap
}
