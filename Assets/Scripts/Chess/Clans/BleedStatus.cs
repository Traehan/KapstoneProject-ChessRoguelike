using UnityEngine;
using Chess;

[DisallowMultipleComponent]
public sealed class BleedStatus : MonoBehaviour
{
    [SerializeField] int stacks;
    public int Stacks => stacks;

    Piece _piece;

    void Awake() => _piece = GetComponent<Piece>();

    public void Add(int amount)
    {
        if (amount <= 0) return;
        stacks += amount;
        GameEvents.OnPieceStatsChanged?.Invoke(_piece);
    }

    public void Tick(ChessBoard board)
    {
        if (stacks <= 0 || _piece == null) return;

        _piece.currentHP -= stacks;
        GameEvents.OnPieceDamaged?.Invoke(_piece, stacks, null);
        GameEvents.OnPieceStatsChanged?.Invoke(_piece);

        if (_piece.currentHP <= 0 && board != null)
            board.RemovePiece(_piece);
    }
}