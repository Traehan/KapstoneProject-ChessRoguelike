using UnityEngine;
using UnityEngine.EventSystems;
using Card;
using Chess;

[DisallowMultipleComponent]
public class StartTroopPopupCardItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] StatusDatabase statusDatabase;
    [SerializeField] CardInspectModal inspectModal;

    Card.Card _runtimeDisplayCard;
    CardView _cardView;

    public void Bind(Card.Card runtimeDisplayCard, StatusDatabase dbOverride = null, CardInspectModal modalOverride = null)
    {
        _runtimeDisplayCard = runtimeDisplayCard;

        if (dbOverride != null)
            statusDatabase = dbOverride;

        if (modalOverride != null)
            inspectModal = modalOverride;

        if (_cardView == null)
            _cardView = GetComponent<CardView>();

        if (_cardView == null)
            _cardView = GetComponentInChildren<CardView>();

        if (_cardView == null)
        {
            Debug.LogWarning("[StartTroopPopupCardItem] No CardView found.");
            return;
        }

        if (_runtimeDisplayCard != null)
            _cardView.Bind(_runtimeDisplayCard);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_runtimeDisplayCard == null)
            return;

        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (inspectModal == null)
        {
            Debug.LogWarning("[StartTroopPopupCardItem] No inspectModal assigned.");
            return;
        }

        inspectModal.Show(_runtimeDisplayCard, statusDatabase);
    }
}