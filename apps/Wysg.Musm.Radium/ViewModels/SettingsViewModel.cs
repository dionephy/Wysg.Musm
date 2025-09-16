using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using System.Threading.Tasks;
using System.Windows;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IRadiumLocalSettings _local;

        [ObservableProperty]
        private string? connectionString;

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand TestCommand { get; }

        public SettingsViewModel() : this(new RadiumLocalSettings()) { }

        public SettingsViewModel(IRadiumLocalSettings local)
        {
            _local = local;
            ConnectionString = _local.ConnectionString ?? "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";
            SaveCommand = new RelayCommand(Save);
            TestCommand = new AsyncRelayCommand(TestAsync);
        }

        private void Save()
        {
            _local.ConnectionString = ConnectionString ?? string.Empty;
            MessageBox.Show("Saved.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task TestAsync()
        {
            try
            {
                await using var con = new NpgsqlConnection(ConnectionString);
                await con.OpenAsync();
                MessageBox.Show("Connection OK.", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed: {ex.Message}", "Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
