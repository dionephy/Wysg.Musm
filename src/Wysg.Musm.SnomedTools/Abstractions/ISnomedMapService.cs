using System.Threading.Tasks;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.SnomedTools.Abstractions
{
    /// <summary>
    /// Service for mapping phrases to SNOMED-CT concepts.
/// </summary>
    public interface ISnomedMapService
    {
        /// <summary>
        /// Cache a SNOMED concept for future reference.
 /// </summary>
    Task CacheConceptAsync(SnomedConcept concept);

        /// <summary>
  /// Map a phrase to a SNOMED concept.
        /// </summary>
  /// <param name="phraseId">Phrase ID to map</param>
        /// <param name="accountId">Account ID or null for global mapping</param>
    /// <param name="conceptId">SNOMED concept ID</param>
   /// <param name="mappingType">Type of mapping (e.g., "exact")</param>
        /// <param name="confidence">Confidence score (0.0 to 1.0)</param>
        /// <param name="notes">Optional notes about the mapping</param>
        /// <param name="mappedBy">User ID who created the mapping (optional)</param>
        /// <returns>True if mapping was successful</returns>
        Task<bool> MapPhraseAsync(
         long phraseId,
            long? accountId,
long conceptId,
string mappingType = "exact",
          decimal? confidence = null,
   string? notes = null,
   long? mappedBy = null);
    }
}
