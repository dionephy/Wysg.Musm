using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
   /// Adapter that bridges Radium's IPhraseService to SnomedTools' IPhraseService interface.
    /// </summary>
    public sealed class PhraseServiceAdapter : Wysg.Musm.SnomedTools.Abstractions.IPhraseService
    {
      private readonly Wysg.Musm.Radium.Services.IPhraseService _radiumService;

   public PhraseServiceAdapter(Wysg.Musm.Radium.Services.IPhraseService radiumService)
        {
     _radiumService = radiumService;
}

        public async Task<IReadOnlyList<Wysg.Musm.SnomedTools.Abstractions.PhraseInfo>> GetAllGlobalPhraseMetaAsync()
{
      var radiumPhrases = await _radiumService.GetAllGlobalPhraseMetaAsync();
 
            // Convert from Radium types to SnomedTools types
 return radiumPhrases.Select(p => new Wysg.Musm.SnomedTools.Abstractions.PhraseInfo(
       p.Id,
         p.AccountId,
  p.Text,
    p.Active,
        p.UpdatedAt,
  p.Rev,
     p.Tags,
         p.TagsSource,
        p.TagsSemanticTag
         )).ToList();
        }

        public async Task<Wysg.Musm.SnomedTools.Abstractions.PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
        {
 var radiumPhrase = await _radiumService.UpsertPhraseAsync(accountId, text, active);
  
       // Convert from Radium type to SnomedTools type
   return new Wysg.Musm.SnomedTools.Abstractions.PhraseInfo(
      radiumPhrase.Id,
    radiumPhrase.AccountId,
         radiumPhrase.Text,
 radiumPhrase.Active,
       radiumPhrase.UpdatedAt,
          radiumPhrase.Rev,
   radiumPhrase.Tags,
         radiumPhrase.TagsSource,
 radiumPhrase.TagsSemanticTag
      );
        }

        public Task RefreshGlobalPhrasesAsync()
        {
    return _radiumService.RefreshGlobalPhrasesAsync();
   }
    }
}
