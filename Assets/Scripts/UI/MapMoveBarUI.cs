using UnityEngine;

public class MapMoveBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] MapGenerator mapGenerator;

    [Header("Slots")]
    [SerializeField] MapMoveSlotUI rookSlot;
    [SerializeField] MapMoveSlotUI bishopSlot;
    [SerializeField] MapMoveSlotUI knightSlot;
    [SerializeField] MapMoveSlotUI queenSlot;

    void Awake()
    {
        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();
    }

    void OnEnable()
    {
        Refresh();
    }

    void Update()
    {
        // Simple first-pass refresh.
        // Totally fine for now since this bar is tiny.
        Refresh();
    }

    public void Refresh()
    {
        var gs = GameSession.I;
        if (gs == null || mapGenerator == null)
            return;

        if (rookSlot != null)
        {
            rookSlot.Bind(
                MapMovementType.Rook,
                gs.rookMapMoveCount,
                gs.selectedMapMovementType == MapMovementType.Rook,
                mapGenerator
            );
        }

        if (bishopSlot != null)
        {
            bishopSlot.Bind(
                MapMovementType.Bishop,
                gs.bishopMapMoveCount,
                gs.selectedMapMovementType == MapMovementType.Bishop,
                mapGenerator
            );
        }

        if (knightSlot != null)
        {
            knightSlot.Bind(
                MapMovementType.Knight,
                gs.knightMapMoveCount,
                gs.selectedMapMovementType == MapMovementType.Knight,
                mapGenerator
            );
        }

        if (queenSlot != null)
        {
            queenSlot.Bind(
                MapMovementType.Queen,
                gs.queenMapMoveCount,
                gs.selectedMapMovementType == MapMovementType.Queen,
                mapGenerator
            );
        }
    }
}