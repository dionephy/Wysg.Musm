using System.Collections.Generic;
using System.Linq;

namespace Wysg.Musm.Radium.Services
{
    public static class TechniqueFormatter
    {
        public static string BuildGroupedDisplay(IEnumerable<TechniqueGroupItem> items)
        {
            if (items == null) return string.Empty;
            // Preserve first-seen order of (prefix,suffix) groups
            var groups = new List<(string prefix, string suffix, List<string> techs)>();
            var seenGroupKeys = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (var it in items.OrderBy(i => i.SequenceOrder))
            {
                var prefix = (it.Prefix ?? string.Empty).Trim();
                var tech = (it.Tech ?? string.Empty).Trim();
                var suffix = (it.Suffix ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(tech)) continue; // skip invalid rows
                var key = prefix + "\u001F" + suffix; // unlikely delimiter
                int idx;
                if (!seenGroupKeys.Contains(key))
                {
                    seenGroupKeys.Add(key);
                    groups.Add((prefix, suffix, new List<string> { tech }));
                }
                else
                {
                    idx = groups.FindIndex(g => g.prefix == prefix && g.suffix == suffix);
                    if (idx >= 0 && !groups[idx].techs.Contains(tech))
                    {
                        groups[idx].techs.Add(tech);
                    }
                }
            }
            // Render groups
            var parts = new List<string>();
            foreach (var g in groups)
            {
                var techList = string.Join(", ", g.techs);
                string head = string.IsNullOrWhiteSpace(g.prefix) ? techList : (g.prefix + " " + techList);
                if (!string.IsNullOrWhiteSpace(g.suffix)) head = head + " " + g.suffix;
                parts.Add(head.Trim());
            }
            return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }

    public sealed record TechniqueGroupItem(string? Prefix, string? Tech, string? Suffix, int SequenceOrder);
}
