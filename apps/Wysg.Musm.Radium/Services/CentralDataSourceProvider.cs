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
        public NpgsqlDataSource Central => _central.Value;
        public NpgsqlDataSource CentralMeta => _centralMeta.Value;

        private readonly IRadiumLocalSettings _settings;
        private readonly Lazy<NpgsqlDataSource> _central;
        private readonly Lazy<NpgsqlDataSource> _centralMeta;
        private bool _disposed;

        public CentralDataSourceProvider(IRadiumLocalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var redacted = string.IsNullOrWhiteSpace(_settings.CentralConnectionString) ? "<empty>" : "provided";
            Debug.WriteLine($"[CentralDataSourceProvider] Raw connection string: {redacted}");

            _central = new Lazy<NpgsqlDataSource>(BuildCentral, isThreadSafe: true);
            _centralMeta = new Lazy<NpgsqlDataSource>(BuildCentralMeta, isThreadSafe: true);
        }

        private NpgsqlDataSource BuildCentral()
        {
            var raw = _settings.CentralConnectionString?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Central DB is not configured. Open Settings and set IRadiumLocalSettings.CentralConnectionString.");

            var tracePg = Environment.GetEnvironmentVariable("RAD_TRACE_PG") == "1";

            var normalBuilder = new NpgsqlConnectionStringBuilder(raw)
            {
                IncludeErrorDetail = true,
                Multiplexing = false,
                KeepAlive = 15,
                NoResetOnClose = false
            };

            // Enforce SSL if not explicitly provided
            if (!(normalBuilder.ContainsKey("Ssl Mode") || normalBuilder.ContainsKey("SslMode") || normalBuilder.ContainsKey("SSL Mode")))
                normalBuilder.SslMode = SslMode.Require;

            if (normalBuilder.Timeout < 8) normalBuilder.Timeout = 8;
            if (normalBuilder.CommandTimeout < 30) normalBuilder.CommandTimeout = 30;
            if (!normalBuilder.ContainsKey("Cancellation Timeout")) normalBuilder["Cancellation Timeout"] = 4000;

            Debug.WriteLine($"[CentralDataSourceProvider] Final connection string: {normalBuilder.ConnectionString}");

            var dsBuilder = new NpgsqlDataSourceBuilder(normalBuilder.ConnectionString);
            if (tracePg) dsBuilder.UseLoggerFactory(new NpgsqlTraceLoggerFactory());
            var ds = dsBuilder.Build();

            Debug.WriteLine($"[PG][DataSource] Central created Host={normalBuilder.Host} Port={normalBuilder.Port} Pooling={normalBuilder.Pooling} NoReset={normalBuilder.NoResetOnClose} SSLMode={normalBuilder.SslMode}");
            return ds;
        }

        private NpgsqlDataSource BuildCentralMeta()
        {
            var raw = _settings.CentralConnectionString?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Central DB is not configured. Open Settings and set IRadiumLocalSettings.CentralConnectionString.");

            var tracePg = Environment.GetEnvironmentVariable("RAD_TRACE_PG") == "1";

            var normalBuilder = new NpgsqlConnectionStringBuilder(raw)
            {
                IncludeErrorDetail = true,
                Multiplexing = false,
                KeepAlive = 15,
                NoResetOnClose = false
            };
            if (!(normalBuilder.ContainsKey("Ssl Mode") || normalBuilder.ContainsKey("SslMode") || normalBuilder.ContainsKey("SSL Mode")))
                normalBuilder.SslMode = SslMode.Require;
            if (normalBuilder.Timeout < 8) normalBuilder.Timeout = 8;
            if (normalBuilder.CommandTimeout < 30) normalBuilder.CommandTimeout = 30;
            if (!normalBuilder.ContainsKey("Cancellation Timeout")) normalBuilder["Cancellation Timeout"] = 4000;

            var metaBuilder = new NpgsqlConnectionStringBuilder(normalBuilder.ConnectionString)
            {
                CommandTimeout = 10
            };
            if (!metaBuilder.ContainsKey("Cancellation Timeout")) metaBuilder["Cancellation Timeout"] = 3000;

            var dsMetaBuilder = new NpgsqlDataSourceBuilder(metaBuilder.ConnectionString);
            if (tracePg) dsMetaBuilder.UseLoggerFactory(new NpgsqlTraceLoggerFactory());
            var ds = dsMetaBuilder.Build();

            Debug.WriteLine($"[PG][DataSource] CentralMeta timeout={metaBuilder.CommandTimeout}s");
            return ds;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { if (_central.IsValueCreated) _central.Value.Dispose(); } catch { }
            try { if (_centralMeta.IsValueCreated) _centralMeta.Value.Dispose(); } catch { }
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
