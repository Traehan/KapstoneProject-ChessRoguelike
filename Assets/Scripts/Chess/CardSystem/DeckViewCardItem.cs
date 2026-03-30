using UnityEngine;
using UnityEngine.EventSystems;
using Card;
using Chess;

[DisallowMultipleComponent]
public class DeckViewCardItem : MonoBehaviour, IPointerClickHandler
{
    [Header("Inspect")]
    [SerializeField] StatusDatabase statusDatabase;

    CardDefinitionSO _definition;
    PieceDefinition _pieceDefinition;
    Card.Card _runtimeDisplayCard;
    CardView _cardView;

    public CardDefinitionSO Definition => _definition;
    public PieceDefinition PieceDefinition => _pieceDefinition;
    public Card.Card RuntimeDisplayCard => _runtimeDisplayCard;

    public void Bind(CardDefinitionSO definition)
    {
        _definition = definition;
        _pieceDefinition = null;

        if (_definition == null)
        {
            Debug.LogWarning("[DeckViewCardItem] Bind(CardDefinitionSO) called with null definition.");
            return;
        }

        _runtimeDisplayCard = new Card.Card(_definition);
        BindToView();
    }

    public void Bind(PieceDefinition pieceDefinition)
    {
        _pieceDefinition = pieceDefinition;
        _definition = null;

        if (_pieceDefinition == null)
        {
            Debug.LogWarning("[DeckViewCardItem] Bind(PieceDefinition) called with null pieceDefinition.");
            return;
        }

        _runtimeDisplayCard = new Card.Card(_pieceDefinition, manaCost: 1);
        BindToView();
    }

    void BindToView()
    {
        if (_cardView == null)
            _cardView = GetComponent<CardView>();

        if (_cardView == null)
            _cardView = GetComponentInChildren<CardView>();

        if (_cardView == null)
        {
            Debug.LogWarning("[DeckViewCardItem] No CardView found on deck item prefab.");
            return;
        }

        _cardView.Bind(_runtimeDisplayCard);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (_runtimeDisplayCard == null)
            return;

        if (CardInspectModal.Instance == null)
        {
            Debug.LogWarning("[DeckViewCardItem] No CardInspectModal in scene.");
            return;
        }

        CardInspectModal.Instance.Show(_runtimeDisplayCard, statusDatabase);
    }
}