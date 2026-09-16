using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;

/// <summary>
/// Simple UI row that displays one innate/keyword ability: icon, name, description.
/// Mirrors PieceUpgradeEntryUI's layout so both lists read consistently in PieceInfoPanel.
/// </summary>
public class PieceAbilityEntryUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    public void Bind(PieceAbilitySO ability)
    {
        if (ability == null) return;

        if (iconImage != null)
        {
            if (ability.icon != null)
            {
                iconImage.sprite = ability.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(ability.displayName)
                ? "Ability"
                : ability.displayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrEmpty(ability.description)
                ? ""
                : ability.description;
        }
    }
}
