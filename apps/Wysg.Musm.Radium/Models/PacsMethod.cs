using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wysg.Musm.Radium.Models
{
    /// <summary>
    /// Represents a PACS method configuration that maps a display name to a PacsService method tag.
    /// PACS methods are stored per-profile and can be customized by users.
    /// </summary>
    public class PacsMethod : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _tag = string.Empty;
        private string _description = string.Empty;
        private bool _isBuiltIn;

        /// <summary>
        /// Display name shown in UI (e.g., "Get selected ID from search results list")
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Method tag used in PacsService and procedure storage (e.g., "GetSelectedIdFromSearchResults")
        /// </summary>
        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        /// <summary>
        /// Optional description of what this method does
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// True if this is a built-in method (cannot be deleted), false for user-defined methods
        /// </summary>
        public bool IsBuiltIn
        {
            get => _isBuiltIn;
            set => SetProperty(ref _isBuiltIn, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
