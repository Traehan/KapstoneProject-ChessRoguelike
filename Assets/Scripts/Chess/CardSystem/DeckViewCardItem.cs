using UnityEngine;
using UnityEngine.EventSystems;
using Card;
using Chess;

[DisallowMultipleComponent]
public class DeckViewCardItem : MonoBehaviour, IPointerClickHandler
{
    [Header("Inspect")]
    [SerializeField] StatusDatabase statusDatabase;
    [SerializeField] GameObject selectedOutline;

    CardDefinitionSO _definition;
    PieceDefinition _pieceDefinition;
    Card.Card _runtimeDisplayCard;
    CardView _cardView;
    
    

    int _runDeckIndex = -1;
    int _armyIndex = -1;
    bool _isSelectableForEvent = false;
    bool _isSelected = false;

    public CardDefinitionSO Definition => _definition;
    public PieceDefinition PieceDefinition => _pieceDefinition;
    public Card.Card RuntimeDisplayCard => _runtimeDisplayCard;
    public int RunDeckIndex => _runDeckIndex;
    public bool IsSelected => _isSelected;
    
    public int ArmyIndex => _armyIndex;

    public void Bind(CardDefinitionSO definition, int runDeckIndex = -1, bool selectableForEvent = false)
    {
        _definition = definition;
        _pieceDefinition = null;
        _runDeckIndex = runDeckIndex;
        _isSelectableForEvent = selectableForEvent;
        _isSelected = false;
        RefreshSelectionVisual();

        if (_definition == null)
        {
            Debug.LogWarning("[DeckViewCardItem] Bind(CardDefinitionSO) called with null definition.");
            return;
        }

        _runtimeDisplayCard = new Card.Card(_definition);
        BindToView();
    }

    public void Bind(PieceDefinition pieceDefinition, int armyIndex = -1, bool selectableForEvent = false)
    {
        _pieceDefinition = pieceDefinition;
        _definition = null;
        _runDeckIndex = -1;
        _armyIndex = armyIndex;
        _isSelectableForEvent = selectableForEvent;
        _isSelected = false;
        RefreshSelectionVisual();

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

    public void SetSelected(bool value)
    {
        _isSelected = value;
        RefreshSelectionVisual();
    }

    void RefreshSelectionVisual()
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(_isSelected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_runtimeDisplayCard == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (CardInspectModal.Instance == null)
            {
                Debug.LogWarning("[DeckViewCardItem] No CardInspectModal in scene.");
                return;
            }

            CardInspectModal.Instance.Show(_runtimeDisplayCard, statusDatabase);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!_isSelectableForEvent)
                return;

            if (DeckViewController.Instance == null)
                return;

            DeckViewController.Instance.OnDeckEventCardClicked(this);
        }
    }
}