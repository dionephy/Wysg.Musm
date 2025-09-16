using System.Linq;
using Npgsql;
using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Services
{
    public class TenantService : ITenantService
    {
        private readonly IRadiumLocalSettings _local;
        private string DefaultConnection =>
            "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";

        public TenantService(IRadiumLocalSettings local)
        {
            _local = local;
        }

        private string GetConnectionString()
            => string.IsNullOrWhiteSpace(_local.ConnectionString) ? DefaultConnection : _local.ConnectionString!;

        public async Task<TenantModel?> GetTenantByCodeAsync(string tenantCode)
        {
            const string sql = "SELECT id, code, name, created_at FROM app.tenant WHERE code = @code";

            try
            {
                await using var connection = new NpgsqlConnection(GetConnectionString());
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("code", tenantCode);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int idOrd = reader.GetOrdinal("id");
                    int codeOrd = reader.GetOrdinal("code");
                    int nameOrd = reader.GetOrdinal("name");
                    int createdOrd = reader.GetOrdinal("created_at");

                    return new TenantModel
                    {
                        Id = reader.GetFieldValue<long>(idOrd),
                        Code = reader.GetFieldValue<string>(codeOrd),
                        Name = reader.GetFieldValue<string>(nameOrd),
                        CreatedAt = reader.GetFieldValue<DateTime>(createdOrd)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                var app = System.Windows.Application.Current;

                void ShowUi()
                {
                    System.Windows.MessageBox.Show(
                        $"Database connection failed.\n{ex.Message}",
                        "Connection Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);

                    OpenSettingsWindow();
                }

                if (app?.Dispatcher?.CheckAccess() == true) ShowUi();
                else app?.Dispatcher?.Invoke(ShowUi);

                return null;
            }
        }

        private static void OpenSettingsWindow()
        {
            var app = System.Windows.Application.Current;

            void ShowDialog()
            {
                var dialog = new Views.SettingsWindow();

                // Pick active window or MainWindow as owner; avoid self-ownership
                var owner = app?.Windows?.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive)
                            ?? app?.MainWindow;

                if (owner != null && !ReferenceEquals(owner, dialog))
                    dialog.Owner = owner;

                dialog.ShowDialog();
            }

            if (app?.Dispatcher?.CheckAccess() == true) ShowDialog();
            else app?.Dispatcher?.Invoke(ShowDialog);
        }

        public async Task<bool> ValidateLoginAsync(LoginRequest request)
        {
            // dev Å×³ÍÆ®´Â ÀÚµ¿ ½ÂÀÎ
            if (request.TenantCode == "dev")
                return true;

            var tenant = await GetTenantByCodeAsync(request.TenantCode);
            return tenant != null;
        }
    }
}