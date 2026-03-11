using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Card;
using Chess;

public class CardView : MonoBehaviour
{
    [Header("Main UI")]
    public Image artImage;              // Full card art for units, blank spell card background for spells
    public TMP_Text titleText;
    public TMP_Text costText;

    [Header("Stats")]
    public TMP_Text healthStat;
    public TMP_Text attackStat;
    public TMP_Text moveStat;
    public Image HealthImage;
    public Image attackImage;
    public Image moveImage;

    [Header("Spell UI")]
    public TMP_Text rulesText;
    public Image SpellIconImage;

    [Header("Spell Defaults")]
    public Sprite defaultSpellCardBackground;

    public void Bind(Card.Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardView] Bind called with null card.");
            return;
        }

        if (titleText != null)
            titleText.text = card.Title;

        if (costText != null)
            costText.text = card.ManaCost.ToString();

        if (card.IsSpellCard())
            BindSpellCard(card);
        else
            BindUnitCard(card);
    }

    void BindUnitCard(Card.Card card)
    {
        var unitPiece = card.GetSummonPieceDefinition();

        if (artImage != null)
        {
            artImage.sprite = card.Art;
            artImage.enabled = (artImage.sprite != null);
        }

        if (SpellIconImage != null)
        {
            SpellIconImage.sprite = null;
            SpellIconImage.enabled = false;
        }

        if (rulesText != null)
            rulesText.gameObject.SetActive(false);

        if (healthStat != null) healthStat.gameObject.SetActive(true);
        if (attackStat != null) attackStat.gameObject.SetActive(true);
        if (moveStat != null) moveStat.gameObject.SetActive(true);

        if (unitPiece != null)
        {
            int hp = ReadInt(unitPiece, "maxHP", "MaxHP", "health", "Health", "hp", "HP", "maxHealth", "MaxHealth", "baseHealth", "BaseHealth");
            int atk = ReadInt(unitPiece, "attack", "Attack", "damage", "Damage", "baseAttack", "BaseAttack");
            int mov = ReadInt(unitPiece, "maxStride", "MaxStride", "stride", "Stride", "movement", "Movement", "move", "Move");

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

        if (healthStat != null)
        {
            healthStat.text = "";
            healthStat.gameObject.SetActive(false);
            HealthImage.gameObject.SetActive(false);
        }

        if (attackStat != null)
        {
            attackStat.text = "";
            attackStat.gameObject.SetActive(false);
            attackImage.gameObject.SetActive(false);
        }

        if (moveStat != null)
        {
            moveStat.text = "";
            moveStat.gameObject.SetActive(false);
            moveImage.gameObject.SetActive(false);
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
}