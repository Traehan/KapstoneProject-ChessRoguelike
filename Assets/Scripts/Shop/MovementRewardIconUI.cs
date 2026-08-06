using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovementRewardIconUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text labelText;

    [Header("Icons")]
    [SerializeField] Sprite rookSprite;
    [SerializeField] Sprite bishopSprite;
    [SerializeField] Sprite knightSprite;
    [SerializeField] Sprite queenSprite;

    public void Bind(MapMovementType type)
    {
        if (iconImage != null)
        {
            iconImage.sprite = GetSpriteFor(type);
            iconImage.enabled = iconImage.sprite != null;
        }

        if (labelText != null)
            labelText.text = type.ToString();
    }

    Sprite GetSpriteFor(MapMovementType type)
    {
        switch (type)
        {
            case MapMovementType.Rook:
                return rookSprite;
            case MapMovementType.Bishop:
                return bishopSprite;
            case MapMovementType.Knight:
                return knightSprite;
            case MapMovementType.Queen:
                return queenSprite;
            default:
                return null;
        }
    }
}