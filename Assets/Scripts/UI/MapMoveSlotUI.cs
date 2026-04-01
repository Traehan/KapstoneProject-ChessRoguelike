using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapMoveSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text countText;
    [SerializeField] GameObject selectedHighlight;
    [SerializeField] Button button;

    [Header("Icons")]
    [SerializeField] Sprite rookSprite;
    [SerializeField] Sprite bishopSprite;
    [SerializeField] Sprite knightSprite;
    [SerializeField] Sprite queenSprite;

    [Header("Visuals")]
    [SerializeField] Color enabledColor = Color.white;
    [SerializeField] Color disabledColor = new Color(1f, 1f, 1f, 0.35f);

    MapMovementType movementType;
    MapGenerator mapGenerator;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveListener(OnClicked);
        button.onClick.AddListener(OnClicked);

        // Prevent child graphics from stealing clicks
        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (countText != null)
            countText.raycastTarget = false;

        if (selectedHighlight != null)
        {
            var highlightImage = selectedHighlight.GetComponent<Image>();
            if (highlightImage != null)
                highlightImage.raycastTarget = false;

            var canvasGroup = selectedHighlight.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;
        }
    }

    public void Bind(
        MapMovementType type,
        int count,
        bool isSelected,
        MapGenerator generator)
    {
        movementType = type;
        mapGenerator = generator;

        if (iconImage != null)
        {
            iconImage.sprite = GetSpriteFor(type);
            iconImage.color = count > 0 ? enabledColor : disabledColor;
        }

        if (countText != null)
        {
            countText.text = count.ToString();
            countText.color = count > 0 ? enabledColor : disabledColor;
        }

        if (selectedHighlight != null)
            selectedHighlight.SetActive(isSelected);

        if (button != null)
            button.interactable = count > 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked();
    }

    void OnClicked()
    {
        Debug.Log($"[MapMoveSlotUI] Clicked slot: {movementType}");

        if (mapGenerator == null)
        {
            Debug.LogWarning("[MapMoveSlotUI] mapGenerator is null.");
            return;
        }

        mapGenerator.SelectMovementType(movementType);
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