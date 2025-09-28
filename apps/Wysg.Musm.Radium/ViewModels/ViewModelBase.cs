using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Wysg.Musm.Radium.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value; Raise(name); return true;
        }
    }

    // Renamed to avoid collision with CommunityToolkit.Mvvm.Input.RelayCommand
    public sealed class SimpleCommand : ICommand
    {
        private readonly System.Action<object?> _execute;
        private readonly System.Predicate<object?>? _can;

        public SimpleCommand(System.Action execute) { _execute = _ => execute(); }
        public SimpleCommand(System.Action execute, System.Func<bool> can) { _execute = _ => execute(); _can = _ => can(); }
        public SimpleCommand(System.Action<object?> execute, System.Predicate<object?>? can = null) { _execute = execute; _can = can; }
        public bool CanExecute(object? parameter) => _can == null || _can(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event System.EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        public void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();
    }
}
