using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Enemy-only piece definition. Carries the passive/trigger abilities (and, via
    /// TryGetMovementDestination overrides, the movement archetype) for this enemy variant,
    /// since enemy variants often share one prefab and can't rely on a prefab-level PieceLoadout
    /// the way player pieces do.
    /// </summary>
    [CreateAssetMenu(menuName = "Chess/Enemy Piece Definition", fileName = "NewEnemyPiece")]
    public class EnemyPieceDefinition : PieceDefinition
    {
        [Header("Abilities")]
        [Tooltip("Passive/trigger abilities for this enemy variant, including movement archetypes " +
                 "(e.g. Chase Closest) authored as PieceAbilitySO.TryGetMovementDestination overrides.")]
        public List<PieceAbilitySO> innateAbilities = new();
    }
}
