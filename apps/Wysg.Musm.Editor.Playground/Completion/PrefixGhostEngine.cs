using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wysg.Musm.Editor.Completion;

namespace Wysg.Musm.Editor.Playground.Completion;

public sealed class PrefixGhostEngine : ICompletionEngine
{
    private readonly string[] _words; // keep order for priority
    public PrefixGhostEngine(IEnumerable<string> words) => _words = words.ToArray();

    public async IAsyncEnumerable<string> StreamAsync(
        CompletionRequest req,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Use last word before trailing whitespace so "no " still yields prefix "no".
        var (prefix, endsWithSpace) = TakeRightWordBeforeWhitespace(req.LeftContext);
        if (prefix.Length < 2) yield break;

        // Find best candidate (prefer "prefix␠..." if user already typed a space)
        string? candidate = null;
        var lower = prefix.ToLowerInvariant();

        if (endsWithSpace)
            candidate = _words.FirstOrDefault(w => StartsWithIgnoreCase(w, lower + " "));
        candidate ??= _words.FirstOrDefault(w => StartsWithIgnoreCase(w, lower));

        if (string.IsNullOrEmpty(candidate)) yield break;

        // Compute how many chars of the candidate the user has already typed,
        // by matching *exactly* against the candidate from its start (case-insensitive).
        int typed = MatchedPrefixLength(candidate, prefix);

        // If caret ended with a space and the candidate *does* have a space next,
        // consume exactly that one space (avoid double-space or off-by-one).
        if (endsWithSpace && typed < candidate.Length && candidate[typed] == ' ')
            typed++;

        if (typed >= candidate.Length) yield break;

        // Remainder to show/insert; ensure trailing space for smooth typing.
        var remainder = candidate.Substring(typed);
        if (!remainder.EndsWith(" ")) remainder += " ";

        ct.ThrowIfCancellationRequested();
        yield return remainder; // single, full chunk → Tab inserts full suggestion
    }

    // Returns (lastWordPrefix, caretEndedWithSpace)
    private static (string prefix, bool endsWithSpace) TakeRightWordBeforeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text)) return (string.Empty, false);

        int i = text.Length - 1;
        bool endsWithSpace = false;

        while (i >= 0 && char.IsWhiteSpace(text[i])) { i--; endsWithSpace = true; }
        if (i < 0) return (string.Empty, endsWithSpace);

        int end = i;
        while (i >= 0 && IsWordChar(text[i])) i--;
        int start = i + 1;

        if (start > end) return (string.Empty, endsWithSpace);
        return (text.Substring(start, end - start + 1), endsWithSpace);
    }

    private static bool StartsWithIgnoreCase(string s, string prefix)
    {
        if (prefix.Length > s.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (char.ToUpperInvariant(s[i]) != char.ToUpperInvariant(prefix[i])) return false;
        }
        return true;
    }

    // How many starting characters of 'candidate' match 'typedPrefix' (case-insensitive)
    private static int MatchedPrefixLength(string candidate, string typedPrefix)
    {
        int n = System.Math.Min(candidate.Length, typedPrefix.Length);
        int i = 0;
        while (i < n && char.ToUpperInvariant(candidate[i]) == char.ToUpperInvariant(typedPrefix[i])) i++;
        return i;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-';
}
