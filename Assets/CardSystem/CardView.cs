using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Card;
using Chess;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Main UI")]
    public Image artImage;
    public TMP_Text titleText;
    public TMP_Text costText;
    public TMP_Text rarityText;
    [Tooltip("Optional 'xN' badge for showing you received multiple copies of this card at once (e.g. the run-start card draft). Hidden whenever SetStackCount isn't called or is given a count <= 1.")]
    public TMP_Text stackCountText;

    [Header("Stats")]
    public GameObject StatsPanel;
    public TMP_Text healthStat;
    public TMP_Text attackStat;
    public TMP_Text moveStat;
    public Image HealthImage;
    public Image attackImage;
    public Image moveImage;
    public TMP_Text Description;

    [Header("Spell UI")]
    public TMP_Text rulesText;
    public Image SpellIconImage;

    [Header("Upgrade Slots")]
    [SerializeField] Image upgradeSlot1Image;
    [SerializeField] Image upgradeSlot2Image;

    [Header("Spell Defaults")]
    public Sprite defaultSpellCardBackground;

    [Header("Spell Targeting Motion")]
    [SerializeField] float targetingLift = 35f;
    [SerializeField] float targetingMoveDuration = 0.12f;
    [SerializeField] AnimationCurve targetingEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Hover Lift")]
    [SerializeField] float hoverLift = 20f;
    [SerializeField] float hoverMoveDuration = 0.12f;
    [SerializeField] AnimationCurve hoverEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool _isSpellTargetingActive;
    bool _isHovering;
    bool _hoverLiftEnabled;
    int _preHoverSiblingIndex = -1;
    RectTransform _rect;
    Vector2 _baseAnchoredPos;
    Coroutine _moveRoutine;
    bool _basePosInitialized;
    DraggablePieceIcon _draggable;

    Card.Card _boundCard;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _draggable = GetComponent<DraggablePieceIcon>();
        CacheBasePosition();
    }

    void OnEnable()
    {
        if (SpellTargetingController.Instance != null)
            SpellTargetingController.Instance.OnSpellTargetingStateChanged += HandleSpellTargetingStateChanged;
    }

    void OnDisable()
    {
        if (SpellTargetingController.Instance != null)
            SpellTargetingController.Instance.OnSpellTargetingStateChanged -= HandleSpellTargetingStateChanged;
    }

    public void Bind(Card.CardDefinitionSO definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("[CardView] BindDefinition called with null definition.");
            return;
        }

        Bind(new Card.Card(definition));
    }

    void CacheBasePosition()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (_rect == null)
            return;

        _baseAnchoredPos = _rect.anchoredPosition;
        _basePosInitialized = true;
    }

    // Called by HandPanel right after computing each card's arc-layout slot position, since under the
    // fan layout the slot moves every time the hand relayouts (draw/discard changes card count).
    // Without this, a cached-once base position goes stale and hover-lift lifts from the wrong spot.
    public void SetHandBasePosition(Vector2 pos)
    {
        _baseAnchoredPos = pos;
        _basePosInitialized = true;

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (_rect == null)
            return;

        if (_isHovering || _isSpellTargetingActive)
            _rect.anchoredPosition = ComputeTargetAnchoredPos();
    }

    // Hover-lift is opt-in and off by default: CardView is shared by many containers (prep panel's
    // GridLayoutGroup, the shop/deck-view's GridLayoutGroup+ScrollRect, reward/recruit popups, etc.) that
    // each manage their own child positions, and this card reordering siblings / writing anchoredPosition
    // on hover fights all of them. Only HandPanel (the battle hand) should ever enable this.
    public void SetHoverLiftEnabled(bool enabled)
    {
        _hoverLiftEnabled = enabled;

        if (enabled || !_isHovering)
            return;

        _isHovering = false;

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        if (_rect != null)
            _rect.anchoredPosition = ComputeTargetAnchoredPos();

        if (_preHoverSiblingIndex >= 0 && _rect != null)
            _rect.SetSiblingIndex(_preHoverSiblingIndex);

        _preHoverSiblingIndex = -1;
    }

    Vector2 ComputeTargetAnchoredPos()
    {
        Vector2 target = _baseAnchoredPos;

        if (_isSpellTargetingActive)
            target += new Vector2(0f, targetingLift);

        if (_isHovering)
            target += new Vector2(0f, hoverLift);

        return target;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hoverLiftEnabled)
            return;

        if (_draggable != null && _draggable.IsDragging)
            return;

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (_rect == null)
            return;

        if (!_basePosInitialized)
            CacheBasePosition();

        if (_isHovering)
            return;

        _isHovering = true;
        _preHoverSiblingIndex = _rect.GetSiblingIndex();
        _rect.SetAsLastSibling();

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(AnimateAnchoredPosition(ComputeTargetAnchoredPos(), hoverMoveDuration, hoverEase));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_hoverLiftEnabled)
            return;

        if (!_isHovering)
            return;

        _isHovering = false;

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(AnimateAnchoredPosition(ComputeTargetAnchoredPos(), hoverMoveDuration, hoverEase, RestoreSiblingIndexAfterHover));
    }

    void RestoreSiblingIndexAfterHover()
    {
        if (_draggable != null && _draggable.IsDragging)
            return;

        if (_preHoverSiblingIndex >= 0 && _rect != null)
            _rect.SetSiblingIndex(_preHoverSiblingIndex);

        _preHoverSiblingIndex = -1;
    }

    public void Bind(Card.Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardView] Bind called with null card.");
            return;
        }

        _boundCard = card;
        CacheBasePosition();

        if (titleText != null)
            titleText.text = card.Title;

        if (costText != null)
            costText.text = card.ManaCost.ToString();

        if (rarityText != null)
            rarityText.text = card.Rarity.ToString();

        SetStackCount(1); // reset any stale "xN" badge from a previous Bind of this same view

        if (card.IsSpellCard())
            BindSpellCard(card);
        else
            BindUnitCard(card);

        SetSpellTargetingActiveImmediate(false);
    }

    /// <summary>Shows an "xN" badge (e.g. for a run-start draft granting multiple copies of one card).
    /// Hidden for count &lt;= 1.</summary>
    public void SetStackCount(int count)
    {
        if (stackCountText == null)
            return;

        bool show = count > 1;
        stackCountText.gameObject.SetActive(show);

        if (show)
            stackCountText.text = $"x{count}";
    }

    void BindUnitCard(Card.Card card)
    {
        var baseUnitPiece = card.GetSummonPieceDefinition();
        var displayUnitPiece = ResolveDisplayUnitPiece(card, baseUnitPiece);

        if (artImage != null)
        {
            artImage.sprite = card.Art;
            artImage.enabled = (artImage.sprite != null);
        }

        if (Description != null)
        {
            Description.gameObject.SetActive(true);
            Description.text = displayUnitPiece != null ? displayUnitPiece.Description : "";
        }

        if (SpellIconImage != null)
        {
            SpellIconImage.sprite = null;
            SpellIconImage.enabled = false;
        }

        if (rulesText != null)
            rulesText.gameObject.SetActive(false);

        if (StatsPanel != null) StatsPanel.gameObject.SetActive(true);
        if (HealthImage != null) HealthImage.gameObject.SetActive(true);
        if (attackImage != null) attackImage.gameObject.SetActive(true);
        if (moveImage != null) moveImage.gameObject.SetActive(true);

        if (healthStat != null) healthStat.gameObject.SetActive(true);
        if (attackStat != null) attackStat.gameObject.SetActive(true);
        if (moveStat != null) moveStat.gameObject.SetActive(true);

        if (displayUnitPiece != null)
        {
            int hp = ReadInt(displayUnitPiece, "maxHP", "MaxHP", "health", "Health", "hp", "HP", "maxHealth", "MaxHealth", "baseHealth", "BaseHealth");
            int atk = ReadInt(displayUnitPiece, "attack", "Attack", "damage", "Damage", "baseAttack", "BaseAttack");
            int mov = ReadInt(displayUnitPiece, "maxStride", "MaxStride", "stride", "Stride", "movement", "Movement", "move", "Move");

            if (healthStat != null) healthStat.text = hp.ToString();
            if (attackStat != null) attackStat.text = atk.ToString();
            if (moveStat != null) moveStat.text = mov.ToString();
        }
        else
        {
            if (healthStat != null) healthStat.text = "-";
            if (attackStat != null) attackStat.text = "-";
            if (moveStat != null) moveStat.text = "-";
        }

        RefreshUpgradeIcons(displayUnitPiece);
    }

    void BindSpellCard(Card.Card card)
    {
        if (artImage != null)
        {
            artImage.sprite = defaultSpellCardBackground;
            artImage.enabled = (artImage.sprite != null);
        }

        if (SpellIconImage != null)
        {
            SpellIconImage.sprite = card.Art;
            SpellIconImage.enabled = (SpellIconImage.sprite != null);
        }

        if (rulesText != null)
        {
            rulesText.gameObject.SetActive(true);
            rulesText.text = card.RulesText;
        }

        if (Description != null)
        {
            Description.text = "";
            Description.gameObject.SetActive(false);
        }

        if (healthStat != null)
        {
            healthStat.text = "";
            healthStat.gameObject.SetActive(false);
        }

        if (attackStat != null)
        {
            attackStat.text = "";
            attackStat.gameObject.SetActive(false);
        }

        if (moveStat != null)
        {
            moveStat.text = "";
            moveStat.gameObject.SetActive(false);
        }

        if (HealthImage != null) HealthImage.gameObject.SetActive(false);
        if (attackImage != null) attackImage.gameObject.SetActive(false);
        if (moveImage != null) moveImage.gameObject.SetActive(false);
        if (StatsPanel != null) StatsPanel.gameObject.SetActive(false);

        RefreshUpgradeIcons(null);
    }

    PieceDefinition ResolveDisplayUnitPiece(Card.Card card, PieceDefinition fallbackPiece)
    {
        if (fallbackPiece == null)
            return null;

        if (GameSession.I == null)
            return fallbackPiece;

        // If this card came from a unit card definition, resolve it to the matching runtime army piece.
        var resolvedFromCardDef = GameSession.I.ResolveDisplayPieceForCard(card.Definition);
        if (resolvedFromCardDef != null)
            return resolvedFromCardDef;

        // If this card was built directly from a runtime piece (army/deck view cases), keep it.
        return fallbackPiece;
    }

    void RefreshUpgradeIcons(PieceDefinition displayPiece)
    {
        IReadOnlyList<PieceUpgradeSO> upgrades = null;

        if (displayPiece != null && GameSession.I != null)
            upgrades = GameSession.I.GetQueuedUpgradesFor(displayPiece);

        SetUpgradeSlotVisual(upgradeSlot1Image, upgrades, 0);
        SetUpgradeSlotVisual(upgradeSlot2Image, upgrades, 1);
    }

    void SetUpgradeSlotVisual(Image targetImage, IReadOnlyList<PieceUpgradeSO> upgrades, int slotIndex)
    {
        if (targetImage == null)
            return;

        PieceUpgradeSO upgrade = null;

        if (upgrades != null && slotIndex >= 0 && slotIndex < upgrades.Count)
            upgrade = upgrades[slotIndex];

        bool hasUpgradeIcon = upgrade != null && upgrade.icon != null;

        targetImage.gameObject.SetActive(hasUpgradeIcon);

        if (hasUpgradeIcon)
        {
            targetImage.sprite = upgrade.icon;
            targetImage.enabled = true;
        }
        else
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
        }
    }

    static int ReadInt(object obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetMemberValue(obj, name, out object value))
            {
                if (value is int i) return i;
                if (value is float f) return Mathf.RoundToInt(f);
                if (value is double d) return (int)Math.Round(d);
            }
        }
        return 0;
    }

    static bool TryGetMemberValue(object obj, string memberName, out object value)
    {
        value = null;
        if (obj == null) return false;

        var t = obj.GetType();

        var field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            value = field.GetValue(obj);
            return true;
        }

        var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanRead)
        {
            value = prop.GetValue(obj);
            return true;
        }

        return false;
    }

    void HandleSpellTargetingStateChanged(Card.Card card, bool active)
    {
        if (_boundCard == null)
            return;

        if (card != _boundCard)
            return;

        SetSpellTargetingActive(active);
    }

    public void SetSpellTargetingActive(bool active)
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (_rect == null)
            return;

        if (!_basePosInitialized)
            CacheBasePosition();

        if (_isSpellTargetingActive == active)
            return;

        _isSpellTargetingActive = active;

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(AnimateAnchoredPosition(ComputeTargetAnchoredPos(), targetingMoveDuration, targetingEase));
    }

    void SetSpellTargetingActiveImmediate(bool active)
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (_rect == null)
            return;

        if (!_basePosInitialized)
            CacheBasePosition();

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        _isSpellTargetingActive = active;

        _rect.anchoredPosition = ComputeTargetAnchoredPos();
    }

    IEnumerator AnimateAnchoredPosition(Vector2 target, float duration, AnimationCurve ease, Action onComplete = null)
    {
        Vector2 start = _rect.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float eased = ease != null ? ease.Evaluate(u) : u;
            _rect.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
            yield return null;
        }

        _rect.anchoredPosition = target;
        _moveRoutine = null;
        onComplete?.Invoke();
    }
}