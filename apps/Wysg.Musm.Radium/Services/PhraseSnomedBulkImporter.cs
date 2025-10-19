using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Bulk import phrase-SNOMED CT mappings from CSV files.
    /// CSV Format: phrase_text,concept_id,mapping_type,confidence,notes
    /// Example: "myocardial infarction",22298006,exact,1.0,"Standard mapping"
    /// </summary>
    public interface IPhraseSnomedBulkImporter
    {
        Task<BulkImportResult> ImportFromCsvAsync(string csvPath, long? accountId, bool createPhrasesIfMissing = false);
        Task<BulkImportResult> PreviewCsvAsync(string csvPath, long? accountId);
    }

    public sealed record BulkImportResult(
        int TotalRows,
        int SuccessfulMappings,
        int FailedMappings,
        int PhrasesCreated,
        int ConceptsCached,
        List<string> Errors
    );

    public sealed class PhraseSnomedBulkImporter : IPhraseSnomedBulkImporter
    {
        private readonly IPhraseService _phraseService;
        private readonly ISnomedMapService _snomedMapService;
        private readonly ISnowstormClient _snowstormClient;

        public PhraseSnomedBulkImporter(
            IPhraseService phraseService,
            ISnomedMapService snomedMapService,
            ISnowstormClient snowstormClient)
        {
            _phraseService = phraseService;
            _snomedMapService = snomedMapService;
            _snowstormClient = snowstormClient;
        }

        public async Task<BulkImportResult> PreviewCsvAsync(string csvPath, long? accountId)
        {
            return await ImportInternalAsync(csvPath, accountId, createPhrasesIfMissing: false, previewOnly: true);
        }

        public async Task<BulkImportResult> ImportFromCsvAsync(string csvPath, long? accountId, bool createPhrasesIfMissing = false)
        {
            return await ImportInternalAsync(csvPath, accountId, createPhrasesIfMissing, previewOnly: false);
        }

        private async Task<BulkImportResult> ImportInternalAsync(
            string csvPath,
            long? accountId,
            bool createPhrasesIfMissing,
            bool previewOnly)
        {
            var errors = new List<string>();
            int totalRows = 0;
            int successfulMappings = 0;
            int failedMappings = 0;
            int phrasesCreated = 0;
            int conceptsCached = 0;

            try
            {
                var lines = await File.ReadAllLinesAsync(csvPath);
                if (lines.Length == 0)
                {
                    errors.Add("CSV file is empty");
                    return new BulkImportResult(0, 0, 0, 0, 0, errors);
                }

                // Skip header row if it looks like a header
                int startRow = 0;
                if (lines[0].ToLowerInvariant().Contains("phrase") && lines[0].ToLowerInvariant().Contains("concept"))
                {
                    startRow = 1;
                    Debug.WriteLine("[BulkImport] Detected header row, skipping");
                }

                for (int i = startRow; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    totalRows++;
                    try
                    {
                        var parts = ParseCsvLine(line);
                        if (parts.Count < 2)
                        {
                            errors.Add($"Row {i + 1}: Invalid format - need at least phrase_text and concept_id");
                            failedMappings++;
                            continue;
                        }

                        string phraseText = parts[0].Trim();
                        string conceptIdStr = parts[1].Trim();
                        string mappingType = parts.Count > 2 ? parts[2].Trim() : "exact";
                        decimal? confidence = null;
                        if (parts.Count > 3 && !string.IsNullOrWhiteSpace(parts[3]))
                        {
                            if (decimal.TryParse(parts[3].Trim(), out var conf))
                                confidence = conf;
                        }
                        string? notes = parts.Count > 4 ? parts[4].Trim() : null;

                        if (string.IsNullOrWhiteSpace(phraseText))
                        {
                            errors.Add($"Row {i + 1}: Phrase text is empty");
                            failedMappings++;
                            continue;
                        }

                        if (!long.TryParse(conceptIdStr, out var conceptId))
                        {
                            errors.Add($"Row {i + 1}: Invalid concept ID '{conceptIdStr}'");
                            failedMappings++;
                            continue;
                        }

                        if (previewOnly)
                        {
                            // Just validate, don't execute
                            successfulMappings++;
                            continue;
                        }

                        // 1. Find or create phrase
                        long phraseId = 0;
                        var existingPhrases = accountId.HasValue
                            ? await _phraseService.GetAllPhraseMetaAsync(accountId.Value)
                            : await _phraseService.GetAllGlobalPhraseMetaAsync();

                        var existingPhrase = existingPhrases.FirstOrDefault(p =>
                            p.Text.Equals(phraseText, StringComparison.OrdinalIgnoreCase));

                        if (existingPhrase != null)
                        {
                            phraseId = existingPhrase.Id;
                            Debug.WriteLine($"[BulkImport] Found existing phrase: {phraseText} (id={phraseId})");
                        }
                        else if (createPhrasesIfMissing)
                        {
                            var newPhrase = await _phraseService.UpsertPhraseAsync(accountId, phraseText, active: true);
                            phraseId = newPhrase.Id;
                            phrasesCreated++;
                            Debug.WriteLine($"[BulkImport] Created new phrase: {phraseText} (id={phraseId})");
                        }
                        else
                        {
                            errors.Add($"Row {i + 1}: Phrase '{phraseText}' not found (use createPhrasesIfMissing=true to auto-create)");
                            failedMappings++;
                            continue;
                        }

                        // 2. Fetch concept from Snowstorm and cache it
                        var concepts = await _snowstormClient.SearchConceptsAsync(conceptIdStr, limit: 1);
                        if (concepts.Count == 0)
                        {
                            errors.Add($"Row {i + 1}: SNOMED concept {conceptIdStr} not found in Snowstorm");
                            failedMappings++;
                            continue;
                        }

                        var concept = concepts[0];
                        await _snomedMapService.CacheConceptAsync(concept);
                        conceptsCached++;
                        Debug.WriteLine($"[BulkImport] Cached concept: {concept.Fsn} ({concept.ConceptIdStr})");

                        // 3. Map phrase to concept
                        await _snomedMapService.MapPhraseAsync(
                            phraseId,
                            accountId,
                            conceptId,
                            mappingType,
                            confidence,
                            notes);

                        successfulMappings++;
                        Debug.WriteLine($"[BulkImport] Mapped phrase '{phraseText}' to concept {conceptIdStr}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {i + 1}: {ex.Message}");
                        failedMappings++;
                        Debug.WriteLine($"[BulkImport] Row {i + 1} failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"CSV import failed: {ex.Message}");
                Debug.WriteLine($"[BulkImport] CSV import failed: {ex.Message}");
            }

            return new BulkImportResult(
                totalRows,
                successfulMappings,
                failedMappings,
                phrasesCreated,
                conceptsCached,
                errors);
        }

        private static List<string> ParseCsvLine(string line)
        {
            var parts = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            parts.Add(current.ToString());
            return parts;
        }
    }
}
