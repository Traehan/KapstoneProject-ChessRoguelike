// Assets/Scripts/Enemies/EnemyBehaviorFactory.cs
using UnityEngine;

namespace Chess
{
    public static class EnemyBehaviorFactory
    {
        public static void EnsureBehaviorFor(Piece piece, DifficultyTier tier)
        {
            if (piece == null) return;

            // If prefab already has a behavior, keep it (designer override)
            if (piece.TryGetComponent<IEnemyBehavior>(out _)) return;

            // Attach by tier (you can expand this switch as you add more sophisticated AIs)
            switch (tier)
            {
                case DifficultyTier.Easy:
                    piece.gameObject.AddComponent<EnemyGreedyCapture>();
                    break;

                case DifficultyTier.Normal:
                    piece.gameObject.AddComponent<EnemyChaseClosest>();
                    break;

                case DifficultyTier.Hard:
                    // Start with chase; later you can implement “avoid threatened tiles”
                    piece.gameObject.AddComponent<EnemyChaseClosest>();
                    // TODO later: piece.gameObject.AddComponent<EnemyAvoidThreats>();
                    break;
            }
        }
    }
}