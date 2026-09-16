using Card;
using Chess;
using TMPro;
using UnityEngine;

public class CardInspectModal : MonoBehaviour
{
    public static CardInspectModal Instance { get; private set; }

    [Header("Root")]
    [SerializeField] GameObject rootPanel;

    [Header("Large Card")]
    [SerializeField] CardView largeCardView;

    [Header("Optional Text")]
    [SerializeField] TMP_Text inspectTitleText;

    [Header("Keyword UI")]
    [SerializeField] DeckKeywordTooltipController keywordTooltipController;

    [Header("Multi-scene Coexistence")]
    [Tooltip("Enable ONLY on the persistent-scene copy (PersistentUI) so it always reclaims Instance no matter " +
             "what order scenes happen to load in this play session. Leave off on any other coexisting copy " +
             "(e.g. UI_Battle's RewardsPanel copy used for inspecting reward cards).")]
    [SerializeField] bool isPrimaryInstance = false;

    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

    void Awake()
    {
        // Multiple CardInspectModals can legitimately coexist (e.g. PersistentUI's copy for
        // browsing the deck, plus UI_Battle's RewardsPanel copy for inspecting reward cards).
        // Instance is only a convenience fallback for callers that don't hold a direct reference,
        // so nobody gets destroyed. isPrimaryInstance always wins the claim regardless of load order.
        if (Instance == null || isPrimaryInstance)
            Instance = this;

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsOpen)
            return;

        // Press Escape to close inspect
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // Right click again anywhere to close inspect
        if (Input.GetMouseButtonDown(1))
        {
            Close();
            return;
        }
    }

    public void Show(Card.Card card, StatusDatabase database)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardInspectModal] Show called with null card.");
            return;
        }

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (largeCardView != null)
            largeCardView.Bind(card);

        if (inspectTitleText != null)
            inspectTitleText.text = card.Title;

        if (keywordTooltipController != null)
            keywordTooltipController.Rebuild(card, database);
    }

    public void Close()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }
}