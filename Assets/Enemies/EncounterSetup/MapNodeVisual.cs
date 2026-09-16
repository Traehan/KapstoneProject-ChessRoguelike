using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button))]
public class MapNodeVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Components")]
    public Image backgroundImage;
    public TextMeshProUGUI nodeTypeText;
    public Image iconImage;

    [Header("Visual States")]
    public Color availableColor = Color.white;
    public Color visitedColor = Color.gray;
    public Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color hoverColor = Color.yellow;
    public Color currentPositionColor = new Color(1f, 0.9f, 0.4f, 1f);

    [Header("Node Type Colors")]
    public Color startColor = new Color(1f, 0.95f, 0.6f);
    public Color encounterColor = new Color(1f, 0.3f, 0.3f);
    public Color shopColor = new Color(0.3f, 1f, 0.3f);
    public Color randomEventColor = new Color(0.3f, 0.3f, 1f);
    public Color bossColor = new Color(1f, 0.8f, 0.2f);

    [Header("Sprites")]
    public Sprite startSprite;
    public Sprite encounterSprite;
    public Sprite shopSprite;
    public Sprite eventSprite;
    public Sprite bossSprite;
    public Sprite removalSprite;
    public Sprite duplicationSprite;

    Button button;
    MapNode nodeData;
    MapGenerator mapGenerator;
    Color currentColor;
    bool isHovering;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnNodeClicked);
    }

    public void Initialize(MapNode node, MapGenerator generator)
    {
        nodeData = node;
        mapGenerator = generator;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (nodeData == null) return;

        bool isHidden = nodeData.nodeType == MapNodeType.Hidden;

        if (backgroundImage != null)
            backgroundImage.enabled = !isHidden;

        if (iconImage != null)
        {
            iconImage.enabled = !isHidden;
            if (!isHidden)
            {
                iconImage.sprite = GetNodeTypeSprite();
                iconImage.color = GetNodeTypeColor();
            }
        }

        if (nodeTypeText != null)
        {
            nodeTypeText.enabled = !isHidden;
            if (!isHidden)
                nodeTypeText.text = GetNodeTypeDisplayName();
        }

        if (isHidden)
        {
            if (button != null)
                button.interactable = false;
            return;
        }

        bool isCurrentPlayerTile = mapGenerator != null && mapGenerator.IsPlayerOnNode(nodeData);

        Color targetColor;
        bool interactable;

        if (isCurrentPlayerTile)
        {
            targetColor = currentPositionColor;
            interactable = false;
        }
        else if (nodeData.isVisited)
        {
            targetColor = visitedColor;
            interactable = false;
        }
        else if (nodeData.isCurrentlyAvailable)
        {
            targetColor = availableColor;
            interactable = true;
        }
        else
        {
            targetColor = unavailableColor;
            interactable = false;
        }

        currentColor = targetColor;

        if (backgroundImage != null)
            backgroundImage.color = isHovering && interactable ? hoverColor : currentColor;

        if (button != null)
            button.interactable = interactable;
    }

    string GetNodeTypeDisplayName()
    {
        if (nodeData == null) return "Node";

        switch (nodeData.nodeType)
        {
            case MapNodeType.Start:
                return "START";
            case MapNodeType.Encounter:
                return "Battle";
            case MapNodeType.Shop:
                return "Shop";
            case MapNodeType.Recruit:
                return "Recruit";
            case MapNodeType.Boss:
                return "BOSS";
            case MapNodeType.RemoveTwoCards:
                return "Purge 2";
            case MapNodeType.DuplicateCard:
                return "Duplicate";
            case MapNodeType.Hidden:
            default:
                return "";
        }
    }

    Sprite GetNodeTypeSprite()
    {
        if (nodeData == null) return null;

        switch (nodeData.nodeType)
        {
            case MapNodeType.Start:
                return startSprite != null ? startSprite : encounterSprite;
            case MapNodeType.Encounter:
                return encounterSprite;
            case MapNodeType.Shop:
                return shopSprite;
            case MapNodeType.Recruit:
                return eventSprite; // reuse your old event sprite for now
            case MapNodeType.Boss:
                return bossSprite;
            case MapNodeType.RemoveTwoCards:
                return removalSprite;
            case MapNodeType.DuplicateCard:
                return duplicationSprite;
            default:
                return null;
        }
    }

    Color GetNodeTypeColor()
    {
        if (nodeData == null) return Color.white;

        switch (nodeData.nodeType)
        {
            case MapNodeType.Start:
                return startColor;
            case MapNodeType.Encounter:
                return encounterColor;
            case MapNodeType.Shop:
                return shopColor;
            case MapNodeType.Recruit:
                return randomEventColor; // can rename later if you want
            case MapNodeType.Boss:
                return bossColor;
            case MapNodeType.RemoveTwoCards:
                return randomEventColor;
            case MapNodeType.DuplicateCard:
                return randomEventColor;
            default:
                return Color.white;
        }
    }

    void OnNodeClicked()
    {
        if (mapGenerator == null || nodeData == null) return;
        if (!nodeData.isCurrentlyAvailable) return;

        mapGenerator.OnNodeSelected(nodeData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        UpdateVisuals();
    }
}