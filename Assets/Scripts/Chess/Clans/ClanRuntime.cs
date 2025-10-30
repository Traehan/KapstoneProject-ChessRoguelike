using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    // Runtime wrapper the TurnManager hands to abilities
    public class ClanRuntime
    {
        public readonly TurnManager tm;
        public readonly ChessBoard board;
        public readonly Team playerTeam;
        public Queen queen;
        public readonly ClanDefinition def;

        public ClanRuntime(TurnManager tm, ChessBoard board, Team team, Queen queen, ClanDefinition def)
        {
            this.tm = tm; this.board = board; this.playerTeam = team; this.queen = queen; this.def = def;
        }
    }
}