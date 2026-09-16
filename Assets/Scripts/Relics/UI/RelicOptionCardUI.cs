using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess
{
    // One pickable relic option shown in the relic selection window.
    public class RelicOptionCardUI : MonoBehaviour
    {
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI rarityText;
        public Button pickButton;

        RelicSO _relic;
        Action<RelicSO> _onPicked;

        public void Bind(RelicSO relic, Action<RelicSO> onPicked)
        {
            _relic = relic;
            _onPicked = onPicked;
            if (relic == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = relic.icon;
                iconImage.enabled = relic.icon != null;
            }

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(relic.displayName) ? relic.name : relic.displayName;

            if (descriptionText != null)
                descriptionText.text = relic.description;

            if (rarityText != null)
                rarityText.text = relic.rarity.ToString();

            if (pickButton != null)
            {
                pickButton.onClick.RemoveAllListeners();
                pickButton.onClick.AddListener(() => _onPicked?.Invoke(_relic));
            }
        }
    }
}
