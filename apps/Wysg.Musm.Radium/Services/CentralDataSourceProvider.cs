using System;
using System.Diagnostics;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public interface ICentralDataSourceProvider : IDisposable
    {
        /// <summary>Primary data source for normal central (cloud) workload queries.</summary>
        NpgsqlDataSource Central { get; }
        /// <summary>Secondary data source with shorter command timeout for lightweight metadata / health probes.</summary>
        NpgsqlDataSource CentralMeta { get; }
    }

    /// <summary>
    /// Builds and exposes shared <see cref="NpgsqlDataSource"/> instances for the central PostgreSQL database.
    ///
    /// Why a provider:
    ///   * Ensures a single Npgsql connection pool per logical purpose instead of each service creating its own builder.
    ///   * Allows a distinct metadata source (shorter CommandTimeout) so detection probes fail fast without impacting
    ///     long-running normal queries.
    ///   * Central place to enable low-level tracing (when RAD_TRACE_PG=1) using a lightweight custom logger factory.
    ///
    /// Configuration heuristics:
    ///   * Enforces SslMode=Require when absent for production safety.
    ///   * Sets KeepAlive to keep idle connections from being dropped by intermediaries (helps free tier cold starts).
    ///   * Adds a "Cancellation Timeout" (if missing) to improve responsiveness on user cancellations.
    ///   * Disables Multiplexing (diagnostic clarity + avoids edge cases with some pg features under heavy dev iteration).
    ///
    /// Disposal:
    ///   Disposes both data sources (releases connection pools). Only called on application shutdown.
    /// </summary>
    public sealed class CentralDataSourceProvider : ICentralDataSourceProvider
    {
        public NpgsqlDataSource Central { get; }
        public NpgsqlDataSource CentralMeta { get; }
        private bool _disposed;

        public CentralDataSourceProvider(IRadiumLocalSettings settings)
        {
            var raw = settings.CentralConnectionString;
            Debug.WriteLine($"[CentralDataSourceProvider] Raw connection string: {raw}");
            var tracePg = Environment.GetEnvironmentVariable("RAD_TRACE_PG") == "1";

            // Base builder for normal workload ------------------------------------------------
            var normalBuilder = new NpgsqlConnectionStringBuilder(raw)
            {
                IncludeErrorDetail = true,          // richer server error messages
                Multiplexing = false,               // simpler debugging (per-connector pipeline)
                KeepAlive = 15,                     // TCP keepalive interval (sec) to detect half-open connections
                NoResetOnClose = false              // allow protocol RESET for pool hygiene
            };
            if (!raw.Contains("sslmode", StringComparison.OrdinalIgnoreCase)) normalBuilder.SslMode = SslMode.Require;
            if (normalBuilder.Timeout < 8) normalBuilder.Timeout = 8;              // connect timeout seconds
            if (normalBuilder.CommandTimeout < 30) normalBuilder.CommandTimeout = 30; // default per-command
            if (!normalBuilder.ContainsKey("Cancellation Timeout")) normalBuilder["Cancellation Timeout"] = 4000; // ms

            Debug.WriteLine($"[CentralDataSourceProvider] Final connection string: {normalBuilder.ConnectionString}");

            // Metadata / health probe builder (shorter execution timeout) ---------------------
            var metaBuilder = new NpgsqlConnectionStringBuilder(normalBuilder.ConnectionString)
            {
                CommandTimeout = 10
            };
            if (!metaBuilder.ContainsKey("Cancellation Timeout")) metaBuilder["Cancellation Timeout"] = 3000;

            // Build primary data source
            var dsBuilder = new NpgsqlDataSourceBuilder(normalBuilder.ConnectionString);
            if (tracePg) dsBuilder.UseLoggerFactory(new NpgsqlTraceLoggerFactory());
            Central = dsBuilder.Build();

            // Build metadata data source
            var dsMetaBuilder = new NpgsqlDataSourceBuilder(metaBuilder.ConnectionString);
            if (tracePg) dsMetaBuilder.UseLoggerFactory(new NpgsqlTraceLoggerFactory());
            CentralMeta = dsMetaBuilder.Build();

            Debug.WriteLine($"[PG][DataSource] Central created Host={normalBuilder.Host} Port={normalBuilder.Port} Pooling={normalBuilder.Pooling} NoReset={normalBuilder.NoResetOnClose} SSLMode={normalBuilder.SslMode}");
            Debug.WriteLine($"[PG][DataSource] CentralMeta timeout={metaBuilder.CommandTimeout}s");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Central.Dispose(); } catch { }
            try { CentralMeta.Dispose(); } catch { }
        }
    }

    /// <summary>
    /// Lightweight logger factory hooking Npgsql internal logging into Debug.WriteLine so developers can observe
    /// wire-level behavior (connection opens, pool events, timeouts) when RAD_TRACE_PG=1.
    /// </summary>
    internal sealed class NpgsqlTraceLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
    {
        public void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new NpgsqlTraceLogger(categoryName);
        public void Dispose() { }
        private sealed class NpgsqlTraceLogger : Microsoft.Extensions.Logging.ILogger
        {
            private readonly string _cat;
            public NpgsqlTraceLogger(string cat) { _cat = cat; }
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true; // env variable gates creation
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                try
                {
                    var msg = formatter != null ? formatter(state, exception) : state?.ToString();
                    Debug.WriteLine($"[PG][Trace][{logLevel}][{_cat}] {msg}{(exception!=null?" | EX="+exception.Message: string.Empty)}");
                }
                catch { }
            }
            private sealed class NullScope : IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
        }
    }
}
