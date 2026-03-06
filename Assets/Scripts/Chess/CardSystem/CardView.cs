using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Chess;

public class CardView : MonoBehaviour
{
    [Header("UI")]
    public Image artImage;
    public TMP_Text titleText;
    public TMP_Text costText;

    [Header("Stats")]
    public TMP_Text healthStat;
    public TMP_Text attackStat;
    public TMP_Text moveStat;

    public void Bind(PieceDefinition def, int apCost = 0)
    {
        if (def == null)
        {
            Debug.LogWarning("[CardView] Bind called with null def.");
            return;
        }

        // Art (your baked card sprite should be in def.icon)
        if (artImage != null)
        {
            artImage.sprite = def.icon;
            artImage.enabled = (artImage.sprite != null);
        }

        // Title / cost
        if (titleText != null) titleText.text = !string.IsNullOrEmpty(def.displayName) ? def.displayName : def.name;
        if (costText != null) costText.text = apCost.ToString();

        // Stats 
        int hp = ReadInt(def, "maxHP", "MaxHP", "health", "Health", "hp", "HP", "maxHealth", "MaxHealth", "baseHealth", "BaseHealth");
        int atk = ReadInt(def, "attack", "Attack", "damage", "Damage", "baseAttack", "BaseAttack");
        int mov = ReadInt(def, "maxStride", "MaxStride", "stride", "Stride", "movement", "Movement", "move", "Move");

        if (healthStat != null) healthStat.text = hp.ToString();
        if (attackStat != null) attackStat.text = atk.ToString();
        if (moveStat != null) moveStat.text = mov.ToString();

        Debug.Log($"[CardView] Bound def='{def.name}' display='{def.displayName}' -> HP={hp} ATK={atk} MOV={mov} icon={(def.icon != null ? def.icon.name : "NULL")}");
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

        // Field
        var field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            value = field.GetValue(obj);
            return true;
        }

        // Property
        var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanRead)
        {
            value = prop.GetValue(obj);
            return true;
        }

        return false;
    }
}