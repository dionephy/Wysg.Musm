using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel
    {
        // Regex pattern for digits-only tokens (integers and decimals)
        private static readonly Regex DigitPattern = new(@"^\d+(\.\d+)?$", RegexOptions.Compiled);
        
        // Regex pattern for date format YYYY-MM-DD
        private static readonly Regex DatePattern = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
        
        // Regex pattern for punctuation-only tokens
        private static readonly Regex PunctuationOnlyPattern = new(@"^[\p{P}\p{S}]+$", RegexOptions.Compiled);

        /// <summary>
        /// Checks if the given text (from Findings or Conclusion editors) contains any unresolved phrases.
        /// Unresolved phrases are words that are NOT in the phrase snapshot (would be colored red),
        /// excluding digits, dates, and punctuation-only tokens.
        /// </summary>
        /// <param name="text">The text to check (findings or conclusion)</param>
        /// <returns>True if there are unresolved phrases (text has red-colored words)</returns>
        public bool HasUnresolvedPhrases(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            var unresolvedWords = GetUnresolvedWords(text);
            return unresolvedWords.Count > 0;
        }

        /// <summary>
        /// Extracts all unresolved words from the given text.
        /// Unresolved words are those NOT in the phrase snapshot, excluding:
        /// - Numbers (digits only, integers and decimals)
        /// - Dates (YYYY-MM-DD format)
        /// - Punctuation-only tokens
        /// </summary>
        /// <param name="text">The text to analyze</param>
        /// <returns>List of distinct unresolved words</returns>
        public IReadOnlyList<string> GetUnresolvedWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            // Get phrase snapshot (case-insensitive)
            var phraseSet = new HashSet<string>(
                CurrentPhraseSnapshot ?? Array.Empty<string>(), 
                StringComparer.OrdinalIgnoreCase);

            var unresolvedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Parse text into words/tokens
            var tokens = TokenizeText(text);
            
            foreach (var token in tokens)
            {
                // Skip empty tokens
                if (string.IsNullOrWhiteSpace(token)) continue;
                
                // Skip numbers (integers and decimals)
                if (DigitPattern.IsMatch(token)) continue;
                
                // Skip dates
                if (DatePattern.IsMatch(token)) continue;
                
                // Skip punctuation-only tokens
                if (PunctuationOnlyPattern.IsMatch(token)) continue;
                
                // Skip if token exists in phrase snapshot
                if (phraseSet.Contains(token)) continue;
                
                // Check multi-word phrase matching (up to 10 words forward)
                // This mirrors PhraseColorizer behavior
                if (IsPartOfMultiWordPhrase(token, tokens, phraseSet)) continue;
                
                // Token is unresolved
                unresolvedWords.Add(token);
            }
            
            return unresolvedWords.OrderBy(w => w).ToList();
        }

        /// <summary>
        /// Tokenizes text into words, handling compound words with connectors (-, /).
        /// Mirrors the logic in PhraseColorizer.FindMatchesInLine.
        /// </summary>
        private static List<string> TokenizeText(string text)
        {
            var tokens = new List<string>();
            int i = 0;
            
            while (i < text.Length)
            {
                // Skip whitespace
                if (char.IsWhiteSpace(text[i])) { i++; continue; }
                
                // Skip standalone punctuation (except hyphen and forward slash)
                if (char.IsPunctuation(text[i]) && text[i] != '-' && text[i] != '/') { i++; continue; }

                // Find word boundaries
                int wordStart = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && 
                       (char.IsLetterOrDigit(text[i]) || text[i] == '-' || text[i] == '/' || text[i] == '.'))
                {
                    i++;
                }

                if (i > wordStart)
                {
                    var token = text.Substring(wordStart, i - wordStart).TrimEnd('.');
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        tokens.Add(token);
                    }
                }
                else
                {
                    i++;
                }
            }
            
            return tokens;
        }

        /// <summary>
        /// Checks if the current token is part of a multi-word phrase that exists in the phrase set.
        /// </summary>
        private static bool IsPartOfMultiWordPhrase(string currentToken, List<string> allTokens, HashSet<string> phraseSet)
        {
            int currentIndex = allTokens.IndexOf(currentToken);
            if (currentIndex < 0) return false;

            // Look ahead up to 9 additional tokens (total 10 words max)
            for (int ahead = 1; ahead <= 9 && currentIndex + ahead < allTokens.Count; ahead++)
            {
                var phrase = string.Join(" ", allTokens.Skip(currentIndex).Take(ahead + 1));
                if (phraseSet.Contains(phrase))
                {
                    return true;
                }
            }

            // Look backward (current token might be part of an earlier phrase)
            for (int back = 1; back <= 9 && currentIndex - back >= 0; back++)
            {
                for (int len = 2; len <= back + 1 && currentIndex - back + len <= allTokens.Count; len++)
                {
                    var phrase = string.Join(" ", allTokens.Skip(currentIndex - back).Take(len));
                    if (phraseSet.Contains(phrase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets unreportified text content from Findings and Conclusion editors,
        /// suitable for export when unresolved phrases are detected.
        /// </summary>
        /// <returns>Combined header, findings, and conclusion text (dereportified)</returns>
        public string GetUnreportifiedTextForExport()
        {
            var (_, findings, conclusion) = GetDereportifiedSections();
            
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(findings))
                parts.Add($"[FINDINGS]\n{findings}");
            
            if (!string.IsNullOrWhiteSpace(conclusion))
                parts.Add($"[CONCLUSION]\n{conclusion}");
            
            return string.Join("\n\n", parts);
        }
    }
}
