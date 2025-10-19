using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Wrapper for bulk SNOMED concept selection.
    /// Adds selection state to a SNOMED concept for multi-select scenarios.
    /// </summary>
    public sealed class SelectableSnomedConcept : INotifyPropertyChanged
    {
        private bool _isSelected;

        public SnomedConcept Concept { get; }

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

        public SelectableSnomedConcept(SnomedConcept concept)
        {
            Concept = concept;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
