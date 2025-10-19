using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Represents an account-specific phrase that can be converted to global.
    /// Used in the "Convert to Global" section of the Global Phrases settings tab.
    /// </summary>
    public sealed class AccountPhraseItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public long Id { get; }
        public long? AccountId { get; }
        public string Text { get; }
        public bool Active { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public AccountPhraseItem(PhraseInfo info)
        {
            Id = info.Id;
            AccountId = info.AccountId;
            Text = info.Text;
            Active = info.Active;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
