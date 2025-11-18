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
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color hoverColor = Color.yellow;
    
    [Header("Node Type Colors")]
    public Color encounterColor = new Color(1f, 0.3f, 0.3f);
    public Color shopColor = new Color(0.3f, 1f, 0.3f);
    public Color randomEventColor = new Color(0.3f, 0.3f, 1f);
    public Color bossColor = new Color(1f, 0.8f, 0.2f); // NEW
    
    [Header("Sprites")]
    public Sprite encounterSprite;
    public Sprite shopSprite;
    public Sprite eventSprite;
    public Sprite bossSprite;
    
    private Button button;
    private MapNode nodeData;
    private MapGenerator mapGenerator;
    private Color currentColor;
    private bool isHovering;

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

        if (nodeTypeText != null)
        {
            nodeTypeText.text = GetNodeTypeDisplayName();
        }

        if (iconImage != null)
        {
            iconImage.sprite = GetNodeTypeSprite();
            iconImage.color = GetNodeTypeColor();
        }

        Color targetColor;
        bool interactable;

        if (nodeData.isVisited)
        {
            targetColor = visitedColor;
            interactable = false;
        }
        else if (nodeData.isLocked)
        {
            targetColor = lockedColor;
            interactable = false;
        }
        else if (nodeData.isCurrentlyAvailable)
        {
            targetColor = availableColor;
            interactable = true;
        }
        else
        {
            targetColor = lockedColor;
            interactable = false;
        }

        currentColor = targetColor;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = isHovering && interactable ? hoverColor : currentColor;
        }

        button.interactable = interactable;
    }

    private string GetNodeTypeDisplayName()
    {
        switch (nodeData.nodeType)
        {
            case MapNodeType.Encounter:
                return "Battle";
            case MapNodeType.Shop:
                return "Shop";
            case MapNodeType.RandomEvent:
                return "Event";
            case MapNodeType.Boss:                     
                return "BOSS";
            default:
                return "Node";
        }
    }

    private Sprite GetNodeTypeSprite()
    {
        switch (nodeData.nodeType)
        {
            case MapNodeType.Encounter:
                return encounterSprite;
            case MapNodeType.Shop:
                return shopSprite;
            case MapNodeType.RandomEvent:
                return eventSprite;
            case MapNodeType.Boss:                     // NEW
                return bossSprite;
            default:
                return null;
        }
    }

    private Color GetNodeTypeColor()
    {
        switch (nodeData.nodeType)
        {
            case MapNodeType.Encounter:
                return encounterColor;
            case MapNodeType.Shop:
                return shopColor;
            case MapNodeType.RandomEvent:
                return randomEventColor;
            case MapNodeType.Boss:
                return bossColor;
            default:
                return Color.white;
        }
    }

    void OnNodeClicked()
    {
        if (mapGenerator != null && nodeData != null && nodeData.isCurrentlyAvailable)
        {
            mapGenerator.OnNodeSelected(nodeData);
        }
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
