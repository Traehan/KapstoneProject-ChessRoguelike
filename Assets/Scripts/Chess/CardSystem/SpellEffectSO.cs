using UnityEngine;

namespace Card
{
    public abstract class SpellEffectSO : ScriptableObject
    {
        [TextArea] public string designerNotes;

        /// <summary>
        /// Return true if the effect resolved successfully.
        /// </summary>
        public abstract bool Resolve(SpellContext context);

        /// <summary>
        /// Undo whatever Resolve() did.
        /// Keep this implemented for command-history support.
        /// </summary>
        public abstract void Undo(SpellContext context);
    }
}