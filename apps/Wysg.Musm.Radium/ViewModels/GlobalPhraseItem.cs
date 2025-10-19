using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Represents a single global phrase in the UI with edit, toggle, and SNOMED mapping capabilities.
    /// </summary>
    public sealed class GlobalPhraseItem : INotifyPropertyChanged
    {
        private readonly GlobalPhrasesViewModel _parent;
        private bool _active;
        private DateTime _updatedAt;
        private long _rev;
        private string _snomedMappingText = string.Empty;
        private string? _snomedSemanticTag;
        private string _text;
        private bool _isEditing;
        private string _editText;

        public long Id { get; }
        
        public string Text
        {
            get => _text;
            private set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged();
                    
                    // Notify all commands that depend on IsEditing
                    ToggleCommand.NotifyCanExecuteChanged();
                    EditCommand.NotifyCanExecuteChanged();
                    SaveEditCommand.NotifyCanExecuteChanged();
                    CancelEditCommand.NotifyCanExecuteChanged();
                    
                    if (_isEditing)
                    {
                        EditText = Text; // Initialize edit text with current value
                    }
                }
            }
        }

        public string EditText
        {
            get => _editText;
            set
            {
                if (_editText != value)
                {
                    _editText = value;
                    OnPropertyChanged();
                    
                    // Notify SaveEditCommand as it depends on EditText not being empty
                    SaveEditCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool Active
        {
            get => _active;
            private set
            {
                if (_active != value)
                {
                    _active = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            private set
            {
                if (_updatedAt != value)
                {
                    _updatedAt = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(UpdatedAtDisplay));
                }
            }
        }

        public long Rev
        {
            get => _rev;
            private set
            {
                if (_rev != value)
                {
                    _rev = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SnomedMappingText
        {
            get => _snomedMappingText;
            internal set
            {
                if (_snomedMappingText != value)
                {
                    _snomedMappingText = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The semantic tag extracted from the SNOMED FSN (e.g., "body structure", "disorder", "procedure").
        /// Used to determine the display color.
        /// </summary>
        public string? SnomedSemanticTag
        {
            get => _snomedSemanticTag;
            internal set
            {
                if (_snomedSemanticTag != value)
                {
                    _snomedSemanticTag = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdatedAtDisplay => UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

        public IAsyncRelayCommand ToggleCommand { get; }
        public IAsyncRelayCommand EditCommand { get; }
        public IAsyncRelayCommand SaveEditCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }

        public GlobalPhraseItem(PhraseInfo info, GlobalPhrasesViewModel parent)
        {
            _parent = parent;
            Id = info.Id;
            _text = info.Text;
            _editText = info.Text;
            _active = info.Active;
            _updatedAt = info.UpdatedAt;
            _rev = info.Rev;

            ToggleCommand = new AsyncRelayCommand(
                async () => await _parent.ToggleActiveAsync(this),
                () => !_parent.IsBusy && !IsEditing
            );

            EditCommand = new AsyncRelayCommand(
                async () => { IsEditing = true; await Task.CompletedTask; },
                () => !_parent.IsBusy && !IsEditing
            );

            SaveEditCommand = new AsyncRelayCommand(
                async () => await _parent.SaveEditAsync(this),
                () => !_parent.IsBusy && IsEditing && !string.IsNullOrWhiteSpace(EditText)
            );

            CancelEditCommand = new RelayCommand(
                () => { IsEditing = false; EditText = Text; },
                () => IsEditing
            );

            DeleteCommand = new AsyncRelayCommand(
                async () => await _parent.DeletePhraseAsync(this),
                () => !_parent.IsBusy && !IsEditing
            );
        }

        public void UpdateFrom(PhraseInfo info)
        {
            Text = info.Text;
            Active = info.Active;
            UpdatedAt = info.UpdatedAt;
            Rev = info.Rev;
            IsEditing = false;
            NotifyCommandsCanExecuteChanged();
        }

        /// <summary>
        /// Notifies all commands to re-evaluate their CanExecute state.
        /// Called when parent IsBusy changes or when item state updates.
        /// </summary>
        public void NotifyCommandsCanExecuteChanged()
        {
            ToggleCommand.NotifyCanExecuteChanged();
            EditCommand.NotifyCanExecuteChanged();
            SaveEditCommand.NotifyCanExecuteChanged();
            CancelEditCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
