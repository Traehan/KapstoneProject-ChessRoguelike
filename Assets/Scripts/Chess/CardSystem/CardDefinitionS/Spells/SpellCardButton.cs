using UnityEngine;
using UnityEngine.EventSystems;
using Card;

public class SpellCardButton : MonoBehaviour, IPointerClickHandler
{
    Card.Card _card;

    public void Init(Card.Card card)
    {
        _card = card;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (_card == null || !_card.IsSpellCard())
            return;

        SpellTargetingController.Instance?.BeginSpellTargeting(_card);
    }
}