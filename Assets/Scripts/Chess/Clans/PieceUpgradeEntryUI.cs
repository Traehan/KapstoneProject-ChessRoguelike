using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;

/// <summary>
/// Simple UI row that displays one upgrade: icon, name, description.
/// </summary>
public class PieceUpgradeEntryUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    public void Bind(PieceUpgradeSO upgrade)
    {
        if (upgrade == null) return;

        if (iconImage != null)
        {
            if (upgrade.icon != null)
            {
                iconImage.sprite = upgrade.icon;
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
            nameText.text = string.IsNullOrEmpty(upgrade.displayName)
                ? "Upgrade"
                : upgrade.displayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrEmpty(upgrade.description)
                ? ""
                : upgrade.description;
        }
    }
}