using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Status Definition", fileName = "STATUS_")]
    public sealed class StatusDefinition : ScriptableObject
    {
        public StatusId id;
        public Sprite icon;
        public string displayName;
        [TextArea] public string description;

        [Header("Display")]
        public bool showStacks = true;
        public int priority = 0; // higher = more important and will occur before the rest
    }
}