using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Card;
using Chess;
using UnityEngine.UI;

public class DeckViewController : MonoBehaviour
{
    public static DeckViewController Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] GameObject rootPanel;

    [Header("UI")]
    [SerializeField] TMP_Text headerText;
    [SerializeField] TMP_Text countText;
    [SerializeField] TMP_Text instructionText;
    [SerializeField] Transform gridParent;
    [SerializeField] GameObject cardItemPrefab;

    [Header("Event Controls")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;

    readonly List<GameObject> _spawned = new();
    readonly List<DeckViewCardItem> _selectedItems = new();

    DeckEditMode _currentMode = DeckEditMode.None;
    Action _onEventFinished;

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

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmCurrentEvent);
            confirmButton.onClick.AddListener(ConfirmCurrentEvent);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelCurrentEvent);
            cancelButton.onClick.AddListener(CancelCurrentEvent);
        }
    }

    public void OpenRunDeckView(string title = "Deck")
    {
        _currentMode = DeckEditMode.None;
        _onEventFinished = null;
        _selectedItems.Clear();

        RebuildCombinedDeckView(title);

        if (instructionText != null)
            instructionText.text = "";

        SetEventButtonsVisible(false);

        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void OpenRemoveTwoMode(Action onFinished = null)
    {
        _currentMode = DeckEditMode.RemoveTwo;
        _onEventFinished = onFinished;
        _selectedItems.Clear();

        RebuildRunDeckOnlyView("Remove Cards", selectableForEvent: true);

        if (instructionText != null)
            instructionText.text = "Choose 2 cards to remove.";

        SetEventButtonsVisible(true);
        RefreshConfirmInteractable();

        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void OpenDuplicateOneMode(Action onFinished = null)
    {
        _currentMode = DeckEditMode.DuplicateOne;
        _onEventFinished = onFinished;
        _selectedItems.Clear();

        RebuildRunDeckOnlyView("Duplicate Card", selectableForEvent: true);

        if (instructionText != null)
            instructionText.text = "Choose 1 card to duplicate.";

        SetEventButtonsVisible(true);
        RefreshConfirmInteractable();

        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void Close()
    {
        if (CardInspectModal.Instance != null)
            CardInspectModal.Instance.Close();

        _selectedItems.Clear();
        _currentMode = DeckEditMode.None;
        _onEventFinished = null;

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

    public void OnDeckEventCardClicked(DeckViewCardItem clickedItem)
    {
        if (_currentMode == DeckEditMode.None || clickedItem == null)
            return;

        if (_currentMode == DeckEditMode.RemoveTwo)
        {
            ToggleSelection(clickedItem, maxSelections: 2);
        }
        else if (_currentMode == DeckEditMode.DuplicateOne)
        {
            ToggleSelection(clickedItem, maxSelections: 1);
        }

        RefreshConfirmInteractable();
    }

    void ToggleSelection(DeckViewCardItem item, int maxSelections)
    {
        if (_selectedItems.Contains(item))
        {
            _selectedItems.Remove(item);
            item.SetSelected(false);
            return;
        }

        if (_selectedItems.Count >= maxSelections)
            return;

        _selectedItems.Add(item);
        item.SetSelected(true);
    }

    void ConfirmCurrentEvent()
    {
        var gs = GameSession.I;
        if (gs == null)
            return;

        if (_currentMode == DeckEditMode.RemoveTwo)
        {
            if (_selectedItems.Count != 2)
                return;

            List<int> indicesToRemove = new List<int>();
            for (int i = 0; i < _selectedItems.Count; i++)
            {
                if (_selectedItems[i] != null && _selectedItems[i].RunDeckIndex >= 0)
                    indicesToRemove.Add(_selectedItems[i].RunDeckIndex);
            }

            indicesToRemove.Sort((a, b) => b.CompareTo(a));

            for (int i = 0; i < indicesToRemove.Count; i++)
            {
                int index = indicesToRemove[i];
                if (index >= 0 && index < gs.CurrentRunDeck.Count)
                    gs.CurrentRunDeck.RemoveAt(index);
            }
        }
        else if (_currentMode == DeckEditMode.DuplicateOne)
        {
            if (_selectedItems.Count != 1)
                return;

            var picked = _selectedItems[0];
            if (picked != null && picked.RunDeckIndex >= 0 && picked.RunDeckIndex < gs.CurrentRunDeck.Count)
            {
                CardDefinitionSO definitionToDuplicate = gs.CurrentRunDeck[picked.RunDeckIndex];
                gs.CurrentRunDeck.Add(definitionToDuplicate);
            }
        }

        Action finish = _onEventFinished;
        Close();
        finish?.Invoke();
    }

    void CancelCurrentEvent()
    {
        Close();
    }

    void RefreshConfirmInteractable()
    {
        if (confirmButton == null)
            return;

        switch (_currentMode)
        {
            case DeckEditMode.RemoveTwo:
                confirmButton.interactable = (_selectedItems.Count == 2);
                break;
            case DeckEditMode.DuplicateOne:
                confirmButton.interactable = (_selectedItems.Count == 1);
                break;
            default:
                confirmButton.interactable = false;
                break;
        }
    }

    void SetEventButtonsVisible(bool visible)
    {
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(visible);

        if (cancelButton != null)
            cancelButton.gameObject.SetActive(visible);
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

                item.Bind(def, runDeckIndex: i, selectableForEvent: false);
                totalCount++;
            }
        }

        if (countText != null)
            countText.text = $"{totalCount} Cards";
    }

    void RebuildRunDeckOnlyView(string title, bool selectableForEvent)
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

                item.Bind(def, runDeckIndex: i, selectableForEvent: selectableForEvent);
                totalCount++;
            }
        }

        if (countText != null)
            countText.text = $"{totalCount} Cards";
    }

    void ClearGrid()
    {
        _selectedItems.Clear();

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