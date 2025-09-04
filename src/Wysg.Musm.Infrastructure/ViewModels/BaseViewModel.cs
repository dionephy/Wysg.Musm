using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Wysg.Musm.Infrastructure.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public IRelayCommand? ErrorHandlingCommand { get; protected set; }
    }
}