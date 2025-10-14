using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel
    {
        private IReadOnlyList<string> _currentPhraseSnapshot = Array.Empty<string>();

        public IReadOnlyList<string> CurrentPhraseSnapshot
        {
            get => _currentPhraseSnapshot;
            set
            {
                _currentPhraseSnapshot = value ?? Array.Empty<string>();
                OnPropertyChanged(nameof(CurrentPhraseSnapshot));
            }
        }

        public async Task LoadPhrasesAsync()
        {
            try
            {
                var accountId = _tenant?.AccountId ?? 0;
                if (accountId <= 0)
                {
                    CurrentPhraseSnapshot = Array.Empty<string>();
                    return;
                }
                // combined (global + account)
                var list = await _phrases.GetCombinedPhrasesAsync(accountId).ConfigureAwait(false);
                CurrentPhraseSnapshot = list ?? Array.Empty<string>();
            }
            catch
            {
                CurrentPhraseSnapshot = Array.Empty<string>();
            }
        }

        public async Task RefreshPhrasesAsync()
        {
            try
            {
                var accountId = _tenant?.AccountId ?? 0;
                if (accountId <= 0) return;
                await _phrases.RefreshGlobalPhrasesAsync().ConfigureAwait(false);
                await _phrases.RefreshPhrasesAsync(accountId).ConfigureAwait(false);
                await LoadPhrasesAsync().ConfigureAwait(false);
            }
            catch { }
        }
    }
}
