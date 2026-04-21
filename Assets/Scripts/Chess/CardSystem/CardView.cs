using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Card;
using Chess;

public class CardView : MonoBehaviour
{
    [Header("Main UI")]
    public Image artImage;
    public TMP_Text titleText;
    public TMP_Text costText;

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

    bool _isSpellTargetingActive;
    RectTransform _rect;
    Vector2 _baseAnchoredPos;
    Coroutine _moveRoutine;
    bool _basePosInitialized;

    Card.Card _boundCard;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
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

        if (card.IsSpellCard())
            BindSpellCard(card);
        else
            BindUnitCard(card);

        SetSpellTargetingActiveImmediate(false);
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

        if (active && !_isSpellTargetingActive)
            _baseAnchoredPos = _rect.anchoredPosition;

        _isSpellTargetingActive = active;

        Vector2 target = active
            ? _baseAnchoredPos + new Vector2(0f, targetingLift)
            : _baseAnchoredPos;

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(AnimateAnchoredPosition(target));
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

        _rect.anchoredPosition = active
            ? _baseAnchoredPos + new Vector2(0f, targetingLift)
            : _baseAnchoredPos;
    }

    IEnumerator AnimateAnchoredPosition(Vector2 target)
    {
        Vector2 start = _rect.anchoredPosition;
        float t = 0f;

        while (t < targetingMoveDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / targetingMoveDuration);
            float eased = targetingEase.Evaluate(u);
            _rect.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
            yield return null;
        }

        _rect.anchoredPosition = target;
        _moveRoutine = null;
    }
}