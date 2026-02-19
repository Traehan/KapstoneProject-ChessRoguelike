using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;

/// <summary>
/// Shows info about the currently-selected player piece:
/// - Icon & name
/// - Current HP / Attack
/// - List of applied upgrades with icon + description
/// </summary>
public class PieceInfoPanel : MonoBehaviour
{
    public static PieceInfoPanel Instance { get; private set; }

    [Header("Root")]
    [Tooltip("The root GameObject for this panel (enable/disable to show/hide).")]
    public GameObject root;

    [Header("Header")]
    public Image pieceImage;
    public TextMeshProUGUI pieceNameText;
    public TextMeshProUGUI statsText;

    [Header("Upgrades")]
    [Tooltip("Parent transform where upgrade entries will be instantiated.")]
    public Transform upgradesContainer;
    [Tooltip("Prefab with PieceUpgradeEntryUI on it.")]
    public PieceUpgradeEntryUI upgradeEntryPrefab;
    [Tooltip("Shown when there are no upgrades on this piece.")]
    public TextMeshProUGUI noUpgradesText;

    PieceRuntime _current;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (root != null)
            root.SetActive(false);
    }
    
    

    /// <summary>Show info for the given piece runtime.</summary>
    public void Show(PieceRuntime runtime)
    {
        if (runtime == null || runtime.Owner == null)
        {
            Hide();
            return;
        }

        _current = runtime;

        if (root != null)
            root.SetActive(true);

        var owner = runtime.Owner;
        var def   = owner.Definition;

        // Icon & name
        if (pieceImage != null)
        {
            if (def != null && def.icon != null)
            {
                pieceImage.sprite = def.icon;
                pieceImage.enabled = true;
            }
            else
            {
                pieceImage.sprite = null;
                pieceImage.enabled = false;
            }
        }

        if (pieceNameText != null)
        {
            if (def != null && !string.IsNullOrEmpty(def.displayName))
                pieceNameText.text = def.displayName;
            else
                pieceNameText.text = owner.name;
        }
        
        // Stats: read from the Piece, which is what combat updates
        if (statsText != null)
        {
            var piece = runtime.Owner;
            if (piece != null)
            {
                int displayedAtk = runtime.GetDisplayedAttack();
                statsText.text = $"ATK: {displayedAtk}\nHP: {piece.currentHP} / {piece.maxHP}";
            }
            else
            {
                // Fallback (shouldn't really happen, but safe)
                statsText.text = $"ATK: {runtime.Attack}\nHP: {runtime.CurrentHP} / {runtime.MaxHP}";
            }
        }


        // Upgrades list
        PopulateUpgrades(runtime);
    }

    /// <summary>Hide the panel and clear state.</summary>
    public void Hide()
    {
        _current = null;

        if (root != null)
            root.SetActive(false);
    }

    void PopulateUpgrades(PieceRuntime runtime)
    {
        // Clear old entries
        if (upgradesContainer != null)
        {
            for (int i = upgradesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(upgradesContainer.GetChild(i).gameObject);
            }
        }

        var list = runtime.Upgrades;
        if (list == null || list.Count == 0)
        {
            if (noUpgradesText != null)
                noUpgradesText.gameObject.SetActive(true);
            return;
        }

        if (noUpgradesText != null)
            noUpgradesText.gameObject.SetActive(false);

        if (upgradesContainer == null || upgradeEntryPrefab == null)
            return;

        foreach (var u in list)
        {
            var entry = Instantiate(upgradeEntryPrefab, upgradesContainer);
            entry.Bind(u);
        }
        
            void OnEnable()
{
    // If your GameEvents lives in Chess namespace, you already have `using Chess;`
    GameEvents.OnPieceMoved += OnAnyPieceMoved;
    GameEvents.OnAttackResolved += OnAttackResolved;
    GameEvents.OnPieceDamaged += OnPieceDamaged;
    GameEvents.OnPieceHealed += OnPieceHealed;
    GameEvents.OnPieceCaptured += OnPieceCaptured;
    GameEvents.OnPieceRestored += OnPieceRestored;

    GameEvents.OnCommandUndone += OnAnyCommandChanged;
    GameEvents.OnCommandRedone += OnAnyCommandChanged;
    GameEvents.OnCommandExecuted += OnAnyCommandChanged;
}

void OnDisable()
{
    GameEvents.OnPieceMoved -= OnAnyPieceMoved;
    GameEvents.OnAttackResolved -= OnAttackResolved;
    GameEvents.OnPieceDamaged -= OnPieceDamaged;
    GameEvents.OnPieceHealed -= OnPieceHealed;
    GameEvents.OnPieceCaptured -= OnPieceCaptured;
    GameEvents.OnPieceRestored -= OnPieceRestored;

    GameEvents.OnCommandUndone -= OnAnyCommandChanged;
    GameEvents.OnCommandRedone -= OnAnyCommandChanged;
    GameEvents.OnCommandExecuted -= OnAnyCommandChanged;
}

bool IsCurrent(Piece p) => _current != null && _current.Owner == p;

void RefreshStatsOnly()
{
    if (_current == null || _current.Owner == null) return;

    var piece = _current.Owner;

    if (statsText != null)
    {
        int displayedAtk = _current.GetDisplayedAttack();
        statsText.text = $"ATK: {displayedAtk}\nHP: {piece.currentHP} / {piece.maxHP}";
    }
}

// ---- Event handlers ----

void OnAnyPieceMoved(Piece piece, Vector2Int from, Vector2Int to, MoveReason reason)
{
    if (IsCurrent(piece)) RefreshStatsOnly();
}

void OnAttackResolved(AttackReport r)
{
    if (IsCurrent(r.attacker) || IsCurrent(r.defender)) RefreshStatsOnly();
}

void OnPieceDamaged(Piece target, int amount, Piece source)
{
    if (IsCurrent(target) || IsCurrent(source)) RefreshStatsOnly();
}

void OnPieceHealed(Piece target, int amount, Piece source)
{
    if (IsCurrent(target) || IsCurrent(source)) RefreshStatsOnly();
}

void OnPieceCaptured(Piece victim, Piece by, Vector2Int at)
{
    if (IsCurrent(victim)) Hide();       // selected piece got removed
    else if (IsCurrent(by)) RefreshStatsOnly();
}

void OnPieceRestored(Piece piece, Vector2Int at)
{
    if (IsCurrent(piece)) RefreshStatsOnly();
}

void OnAnyCommandChanged(IGameCommand cmd)
{
    // safest: undo/redo often changes HP/fortify/position.
    if (_current != null) RefreshStatsOnly();
}

    }
}
