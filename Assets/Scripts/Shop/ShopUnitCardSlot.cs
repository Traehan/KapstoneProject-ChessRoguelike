using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;
using Card;

public class ShopUnitCardSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CardView cardView;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject emptyState;

    private PieceDefinition pieceItem;
    private int price;
    private ShopManager shopManager;
    private bool isPurchased;

    void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void Bind(PieceDefinition piece, int cost, ShopManager manager)
    {
        pieceItem = piece;
        price = cost;
        shopManager = manager;
        isPurchased = false;

        if (emptyState != null)
            emptyState.SetActive(false);

        if (pieceItem != null && cardView != null)
        {
            Card.Card runtimeCard = new Card.Card(pieceItem, manaCost: 1);
            cardView.Bind(runtimeCard);
        }

        if (priceText != null)
            priceText.text = price.ToString();

        RefreshButton();
    }

    public void Clear()
    {
        pieceItem = null;
        price = 0;
        shopManager = null;
        isPurchased = false;

        if (priceText != null)
            priceText.text = "";

        if (emptyState != null)
            emptyState.SetActive(true);

        RefreshButton();
    }

    void RefreshButton()
    {
        if (buyButton == null)
            return;

        bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(price);
        buyButton.interactable = !isPurchased && pieceItem != null && canAfford;
    }

    void OnBuyClicked()
    {
        if (isPurchased || pieceItem == null || shopManager == null)
            return;

        if (shopManager.OnPiecePurchased(pieceItem, price))
        {
            isPurchased = true;
            gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChanged;
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    void OnCoinsChanged(int newAmount)
    {
        RefreshButton();
    }
}