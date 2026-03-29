namespace Chess
{
    public enum SoundEventId
    {
        None = 0,

        // UI / menus
        UIButtonClick,
        UICancel,
        UIHover,
        CardDraw,
        CardSelect,
        CardDeselect,

        // Phase / flow
        PhasePreparation,
        PhaseSpell,
        PhasePlayerTurn,
        PhaseEnemyTurn,
        EncounterWin,
        EncounterLose,

        // Board / units
        PieceMove,
        PieceAttack,
        PieceHit,
        PieceDeath,
        PieceSpawn,

        // Spells / cards
        SpellCastStart,
        SpellCastResolve,
        SpellCastCancel,
        UnitCardPlayed,

        // Status / feedback
        FortifyGain,
        BleedApplied,

        // Music
        MusicMenu,
        MusicBattle
    }
}