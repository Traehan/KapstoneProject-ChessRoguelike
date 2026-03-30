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

    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (rootPanel != null)
            rootPanel.SetActive(false);
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