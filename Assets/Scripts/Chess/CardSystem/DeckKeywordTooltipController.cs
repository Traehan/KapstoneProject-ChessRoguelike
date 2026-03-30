using System.Collections.Generic;
using System.Text;
using Card;
using Chess;
using TMPro;
using UnityEngine;

public class DeckKeywordTooltipController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform contentRoot;
    [SerializeField] GameObject tooltipRowPrefab;
    [SerializeField] TMP_Text emptyText;

    readonly List<GameObject> _spawned = new();

    static readonly Dictionary<string, StatusId> KeywordToStatus = new()
    {
        { "bleed", StatusId.Bleed },
        { "fortify", StatusId.Fortify },
        { "retaliate", StatusId.Retaliate }
    };

    public void Rebuild(Card.Card card, StatusDatabase database)
    {
        Clear();

        if (card == null || database == null)
        {
            ShowEmpty("No keyword data.");
            return;
        }

        var inspectText = BuildInspectText(card);
        if (string.IsNullOrWhiteSpace(inspectText))
        {
            ShowEmpty("No keywords.");
            return;
        }

        HashSet<StatusId> found = new();

        string lower = inspectText.ToLowerInvariant();

        foreach (var kv in KeywordToStatus)
        {
            if (!lower.Contains(kv.Key))
                continue;

            if (!found.Add(kv.Value))
                continue;

            var def = database.Get(kv.Value);
            if (def == null)
                continue;

            SpawnRow(def);
        }

        if (found.Count == 0)
            ShowEmpty("No keywords.");
    }

    string BuildInspectText(Card.Card card)
    {
        StringBuilder sb = new();

        if (!string.IsNullOrWhiteSpace(card.Title))
            sb.AppendLine(card.Title);

        if (!string.IsNullOrWhiteSpace(card.RulesText))
            sb.AppendLine(card.RulesText);

        var piece = card.GetSummonPieceDefinition();
        if (piece != null && !string.IsNullOrWhiteSpace(piece.Description))
            sb.AppendLine(piece.Description);

        return sb.ToString();
    }

    void SpawnRow(StatusDefinition def)
    {
        if (contentRoot == null || tooltipRowPrefab == null || def == null)
            return;

        var go = Instantiate(tooltipRowPrefab, contentRoot);
        _spawned.Add(go);

        var row = go.GetComponent<DeckKeywordTooltipRow>();
        if (row == null)
            row = go.AddComponent<DeckKeywordTooltipRow>();

        row.Bind(def);
    }

    void ShowEmpty(string message)
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(true);
            emptyText.text = message;
        }
    }

    void Clear()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i]);
        }

        _spawned.Clear();

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
            emptyText.text = "";
        }
    }
}