using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckKeywordTooltipRow : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;

    public void Bind(StatusDefinition def)
    {
        if (def == null)
        {
            Debug.LogWarning("[DeckKeywordTooltipRow] Bind called with null StatusDefinition.");
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = def.icon;
            iconImage.enabled = (def.icon != null);
        }

        if (nameText != null)
            nameText.text = def.displayName;

        if (descriptionText != null)
            descriptionText.text = def.description;
    }
}