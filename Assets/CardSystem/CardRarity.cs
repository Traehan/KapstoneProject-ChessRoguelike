namespace Card
{
    // Blight = temporary junk cards (the Slay-the-Spire "Status" equivalent, e.g. Wound/Dazed) -
    // named Blight instead of Status to avoid clashing with Chess.StatusId/StatusController.
    // Curse = permanent negative deck cards. Neither is meant to appear in normal reward/shop pools -
    // they're granted directly (relic effects, events), same as how Slay the Spire never drafts them.
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Blight,
        Curse
    }
}
