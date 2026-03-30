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
        if (rootPanel != null && rootPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Show(Card.Card card, StatusDatabase statusDatabase)
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
            keywordTooltipController.Rebuild(card, statusDatabase);
    }

    public void Close()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }
}