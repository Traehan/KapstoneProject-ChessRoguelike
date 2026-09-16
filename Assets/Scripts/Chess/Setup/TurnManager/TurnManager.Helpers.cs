using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        IEnumerable<Piece> EnumeratePlayerPieces()
        {
            foreach (var p in FindObjectsOfType<Piece>())
                if (p.Team == playerTeam && board.ContainsPiece(p))
                    yield return p;
        }

        void NotifyAllPlayerPieceRuntimes_BeginTurn()
        {
            foreach (var p in EnumeratePlayerPieces())
                p.GetComponent<PieceRuntime>()?.Notify_BeginPlayerTurn();
        }

        void NotifyAllPlayerPieceRuntimes_EndTurn()
        {
            foreach (var p in EnumeratePlayerPieces())
                p.GetComponent<PieceRuntime>()?.Notify_EndPlayerTurn();
        }

        void NotifyAllPieceRuntimes_BeginEnemyTurn()
        {
            foreach (var p in board.GetAllPieces())
                p.GetComponent<PieceRuntime>()?.Notify_BeginEnemyTurn();
        }

        void NotifyAllPieceRuntimes_EndEnemyTurn()
        {
            foreach (var p in board.GetAllPieces())
                p.GetComponent<PieceRuntime>()?.Notify_EndEnemyTurn();
        }

        void EnsureEncounterRunnerBound()
        {
            if (encounterRunner == null)
                encounterRunner = FindObjectOfType<EncounterRunner>();
        }
    }
}