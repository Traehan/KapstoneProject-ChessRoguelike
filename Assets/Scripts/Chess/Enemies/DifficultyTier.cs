// Assets/Scripts/Enemies/DifficultyTier.cs
namespace Chess
{
    public enum DifficultyTier
    {
        Easy,       // basic greedy (current feel)
        Normal,     // chase closest (smarter)
        Hard        // chase + you can later add avoidance/“don’t walk into range” heuristics
    }
}