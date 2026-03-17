using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Draw Cards", fileName = "FX_DrawCards")]
    public class DrawCardsEffectSO : SpellEffectSO
    {
        [Min(1)] public int drawCount = 1;

        int _cardsDrawn;

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.DeckManager == null)
                return false;

            int before = context.DeckManager.Hand.Count;
            context.DeckManager.Draw(drawCount);
            int after = context.DeckManager.Hand.Count;

            _cardsDrawn = Mathf.Max(0, after - before);
            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null || context.DeckManager == null || _cardsDrawn <= 0)
                return;

            for (int i = 0; i < _cardsDrawn; i++)
            {
                if (context.DeckManager.Hand.Count == 0)
                    break;

                var lastIndex = context.DeckManager.Hand.Count - 1;
                var card = context.DeckManager.Hand[lastIndex];
                context.DeckManager.Hand.RemoveAt(lastIndex);

                GameEvents.OnCardRemovedFromHand?.Invoke(card);

                context.DeckManager.DrawPile.Insert(0, card);
            }

            _cardsDrawn = 0;
        }
    }
}