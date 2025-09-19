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
        private string? localConnectionString;

        // Back-compat binding
        public string? ConnectionString
        {
            get => LocalConnectionString;
            set => LocalConnectionString = value;
        }

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand TestLocalCommand { get; }

        public SettingsViewModel() : this(new RadiumLocalSettings()) { }

        public SettingsViewModel(IRadiumLocalSettings local)
        {
            _local = local;
            LocalConnectionString = _local.LocalConnectionString ?? "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";
            SaveCommand = new RelayCommand(Save);
            TestLocalCommand = new AsyncRelayCommand(TestLocalAsync);
        }

        private void Save()
        {
            _local.LocalConnectionString = LocalConnectionString ?? string.Empty;
            MessageBox.Show("Saved.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task TestLocalAsync()
        {
            await TestAsync(LocalConnectionString, "Local");
        }

        private static async Task TestAsync(string? cs, string label)
        {
            try
            {
                await using var con = new NpgsqlConnection(cs);
                await con.OpenAsync();
                MessageBox.Show($"{label} connection OK.", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"{label} failed: {ex.Message}", "Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
