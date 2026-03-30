using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Card;
using Chess;

public class DeckViewController : MonoBehaviour
{
    public static DeckViewController Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] GameObject rootPanel;

    [Header("UI")]
    [SerializeField] TMP_Text headerText;
    [SerializeField] TMP_Text countText;
    [SerializeField] Transform gridParent;
    [SerializeField] GameObject cardItemPrefab;

    readonly List<GameObject> _spawned = new();

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

    public void OpenRunDeckView(string title = "Deck")
    {
        if (GameSession.I == null)
        {
            Debug.LogWarning("[DeckViewController] No GameSession found.");
            return;
        }

        RebuildCombinedDeckView(title);

        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void Close()
    {
        if (CardInspectModal.Instance != null)
            CardInspectModal.Instance.Close();

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void ToggleRunDeckView(string title = "Deck")
    {
        if (rootPanel != null && rootPanel.activeSelf)
        {
            Close();
            return;
        }

        OpenRunDeckView(title);
    }

    void RebuildCombinedDeckView(string title)
    {
        ClearGrid();

        if (headerText != null)
            headerText.text = title;

        if (gridParent == null || cardItemPrefab == null)
        {
            Debug.LogWarning("[DeckViewController] Missing gridParent or cardItemPrefab.");
            return;
        }

        int totalCount = 0;

        // 1. Show army first (queen + starting troop + any other prep units)
        var army = GameSession.I.CurrentArmy;
        if (army != null)
        {
            for (int i = 0; i < army.Count; i++)
            {
                var pieceDef = army[i];
                if (pieceDef == null)
                    continue;

                var go = Instantiate(cardItemPrefab, gridParent);
                _spawned.Add(go);

                var item = go.GetComponent<DeckViewCardItem>();
                if (item == null)
                    item = go.AddComponent<DeckViewCardItem>();

                item.Bind(pieceDef);
                totalCount++;
            }
        }

        // 2. Then show the persistent run deck cards
        var runDeck = GameSession.I.CurrentRunDeck;
        if (runDeck != null)
        {
            for (int i = 0; i < runDeck.Count; i++)
            {
                var def = runDeck[i];
                if (def == null)
                    continue;

                var go = Instantiate(cardItemPrefab, gridParent);
                _spawned.Add(go);

                var item = go.GetComponent<DeckViewCardItem>();
                if (item == null)
                    item = go.AddComponent<DeckViewCardItem>();

                item.Bind(def);
                totalCount++;
            }
        }

        if (countText != null)
            countText.text = $"{totalCount} Cards";
    }

    void ClearGrid()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i]);
        }

        _spawned.Clear();

        if (gridParent == null)
            return;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);
    }
}