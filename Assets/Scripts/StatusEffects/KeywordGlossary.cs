using System.Collections.Generic;

namespace Chess
{
    // Shared keyword-scanning logic used by any tooltip that wants to surface gameplay keywords
    // (Bleed, Fortify, Retaliate, Incant, Rally, Shield, ...) found in arbitrary card/relic text.
    // Pulled out of DeckKeywordTooltipController so the card tooltip and the relic tooltip (and any
    // future tooltip) share one keyword list instead of maintaining separate copies.
    public static class KeywordGlossary
    {
        static readonly Dictionary<string, StatusId> KeywordToStatus = new()
        {
            { "bleed", StatusId.Bleed },
            { "fortify", StatusId.Fortify },
            { "retaliate", StatusId.Retaliate },
            { "incant", StatusId.Incant },
            { "rally", StatusId.Rally },
            { "shield", StatusId.Shield }
        };

        // Scans the given text for any recognized keyword and returns the matching StatusDefinitions
        // (deduplicated, one entry per StatusId even if the keyword appears multiple times).
        public static List<StatusDefinition> FindInText(string text, StatusDatabase database)
        {
            var results = new List<StatusDefinition>();
            if (string.IsNullOrWhiteSpace(text) || database == null)
                return results;

            string lower = text.ToLowerInvariant();

            HashSet<StatusId> found = new();

            foreach (var kv in KeywordToStatus)
            {
                if (!lower.Contains(kv.Key))
                    continue;

                if (!found.Add(kv.Value))
                    continue;

                var def = database.Get(kv.Value);
                if (def == null)
                    continue;

                results.Add(def);
            }

            return results;
        }
    }
}
