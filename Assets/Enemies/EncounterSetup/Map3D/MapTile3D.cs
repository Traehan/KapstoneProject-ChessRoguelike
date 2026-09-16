// Assets/Enemies/EncounterSetup/Map3D/MapTile3D.cs
//
// World-space replacement for MapNodeVisual. Same public shape (Initialize/UpdateVisuals, the same
// color/sprite fields) so MapGenerator barely changes, but rendered as a 3D tile (Renderer + Collider,
// colored via MaterialPropertyBlock like Chess.Tile) with a child billboard SpriteRenderer standee for the
// node-type icon instead of a UI Image/Button.

using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class MapTile3D : MonoBehaviour
{
    [Header("Visual Components")]
    [Tooltip("Base tile renderer, colored via MaterialPropertyBlock (same technique as Chess.Tile).")]
    public Renderer baseRenderer;
    [Tooltip("Child billboard sprite showing the node-type icon.")]
    public SpriteRenderer standeeRenderer;

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

    [Header("Gauntlet Badges (corner accents, shown only when node.isGauntlet)")]
    [Tooltip("Small billboard badge showing the rolled reward type.")]
    public SpriteRenderer rewardBadgeRenderer;
    [Tooltip("Small billboard badge showing the rolled challenge type.")]
    public SpriteRenderer challengeBadgeRenderer;
    [Tooltip("Indexed by GauntletRewardType (Gold, QueenMove, Relic). Placeholder/empty until real art exists.")]
    public Sprite[] rewardIconSprites = new Sprite[3];
    [Tooltip("Indexed by GauntletChallengeType (StatBoost, Swarm, HarderEnemy, ManaHandicap, EnergyHandicap). Placeholder/empty until real art exists.")]
    public Sprite[] challengeIconSprites = new Sprite[5];

    MapNode nodeData;
    MapGenerator mapGenerator;
    Color currentColor;
    bool isHovering;
    bool isInteractable;

    void Awake()
    {
        if (baseRenderer == null)
            baseRenderer = GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        // Billboard: match the map camera's rotation so the icon always faces the viewer, the same
        // "rotate to match camera" approach used for simple 2.5D standees.
        if (standeeRenderer == null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        standeeRenderer.transform.rotation = cam.transform.rotation;

        if (rewardBadgeRenderer != null)
            rewardBadgeRenderer.transform.rotation = cam.transform.rotation;

        if (challengeBadgeRenderer != null)
            challengeBadgeRenderer.transform.rotation = cam.transform.rotation;
    }

    public void Initialize(MapNode node, MapGenerator generator)
    {
        nodeData = node;
        mapGenerator = generator;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (nodeData == null)
            return;

        bool isHidden = nodeData.nodeType == MapNodeType.Hidden;

        // Hidden nodes (off the player's chosen path) are fully disabled: no visual, no collider/click.
        if (gameObject.activeSelf != !isHidden)
            gameObject.SetActive(!isHidden);

        if (isHidden)
            return;

        if (standeeRenderer != null)
        {
            standeeRenderer.sprite = GetNodeTypeSprite();
            standeeRenderer.color = GetNodeTypeColor();
        }

        UpdateGauntletBadges();

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
        isInteractable = interactable;

        ApplyBaseColor(isHovering && interactable ? hoverColor : currentColor);
    }

    void ApplyBaseColor(Color c)
    {
        if (baseRenderer == null)
            return;

        var mpb = new MaterialPropertyBlock();
        baseRenderer.GetPropertyBlock(mpb);

        // Built-in/Standard:
        mpb.SetColor("_Color", c);
        // URP/HDRP Lit:
        mpb.SetColor("_BaseColor", c);

        baseRenderer.SetPropertyBlock(mpb);
    }

    void UpdateGauntletBadges()
    {
        bool showBadges = nodeData != null && nodeData.isGauntlet && nodeData.nodeType == MapNodeType.Encounter;

        if (rewardBadgeRenderer != null)
        {
            rewardBadgeRenderer.gameObject.SetActive(showBadges);
            if (showBadges)
                rewardBadgeRenderer.sprite = GetSpriteForIndex(rewardIconSprites, (int)nodeData.gauntletReward);
        }

        if (challengeBadgeRenderer != null)
        {
            challengeBadgeRenderer.gameObject.SetActive(showBadges);
            if (showBadges)
                challengeBadgeRenderer.sprite = GetSpriteForIndex(challengeIconSprites, (int)nodeData.gauntletChallenge);
        }
    }

    Sprite GetSpriteForIndex(Sprite[] sprites, int index)
    {
        if (sprites == null || index < 0 || index >= sprites.Length)
            return null;
        return sprites[index];
    }

    Sprite GetNodeTypeSprite()
    {
        if (nodeData == null)
            return null;

        switch (nodeData.nodeType)
        {
            case MapNodeType.Start:
                return startSprite != null ? startSprite : encounterSprite;
            case MapNodeType.Encounter:
                return encounterSprite;
            case MapNodeType.Shop:
                return shopSprite;
            case MapNodeType.Recruit:
                return eventSprite; // reuse the old event sprite for now
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
        if (nodeData == null)
            return Color.white;

        switch (nodeData.nodeType)
        {
            case MapNodeType.Start:
                return startColor;
            case MapNodeType.Encounter:
                return encounterColor;
            case MapNodeType.Shop:
                return shopColor;
            case MapNodeType.Recruit:
                return randomEventColor;
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

    // Requires a Camera in the scene (the map camera rig) for Unity's built-in mouse-picking messages
    // (OnMouseDown/Enter/Exit) to fire against this tile's Collider.
    void OnMouseDown()
    {
        // OnMouseDown is a raw Physics raycast, independent of uGUI - guard against clicks meant for an
        // overlapping Canvas panel (dev tools, recruit panel, etc.) also selecting the tile underneath.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (mapGenerator == null || nodeData == null)
            return;
        if (!nodeData.isCurrentlyAvailable)
            return;

        mapGenerator.OnNodeSelected(nodeData);
    }

    void OnMouseEnter()
    {
        isHovering = true;
        UpdateVisuals();
    }

    void OnMouseExit()
    {
        isHovering = false;
        UpdateVisuals();
    }
}
