using System;
using System.Collections.Generic;
using System.Linq;

namespace Wysg.Musm.Editor.Snippets;

/// <summary>
/// Combines laterality tokens. E.g. ["left insula","right insula"] -> "bilateral insula".
/// Also formats lists: "A, B and C".
/// </summary>
public static class LateralityCombiner
{
    public static string CombineFromText(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // Split by comma or "and" (very simple parser; adjust if you use semicolons)
        var tokens = raw
            .Replace(" and ", ",", StringComparison.OrdinalIgnoreCase)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0);

        return Combine(tokens);
    }

    public static string Combine(IEnumerable<string> tokens)
    {
        var sidesByBase = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var singles = new List<string>();

        foreach (var token in tokens)
        {
            var parts = token.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            // Handle "left X", "right X", "bilateral X"
            if (parts.Length == 2)
            {
                var head = parts[0].ToLowerInvariant();
                var @base = parts[1];

                if (head is "left" or "right")
                {
                    if (!sidesByBase.TryGetValue(@base, out var set))
                        sidesByBase[@base] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    set.Add(head);
                    continue;
                }
                if (head == "bilateral")
                {
                    // Bilateral stands alone, we don't need to track sides
                    singles.Add(token);
                    continue;
                }
            }

            // Plain entry
            singles.Add(token);
        }

        var result = new List<string>();

        foreach (var (baseName, sides) in sidesByBase)
        {
            if (sides.Contains("left") && sides.Contains("right"))
                result.Add("bilateral " + baseName);
            else
                result.Add($"{sides.First()} {baseName}");
        }

        result.AddRange(singles);

        return result.Count switch
        {
            0 => string.Empty,
            1 => result[0],
            2 => $"{result[0]} and {result[1]}",
            _ => string.Join(", ", result.Take(result.Count - 1)) + ", and " + result.Last()
        };
    }
}
