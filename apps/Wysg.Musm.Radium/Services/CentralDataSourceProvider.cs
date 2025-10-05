using System;
using System.Diagnostics;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public interface ICentralDataSourceProvider : IDisposable
    {
        NpgsqlDataSource Central { get; }
        NpgsqlDataSource CentralMeta { get; } // short-timeout metadata / health-check source
    }

    public sealed class CentralDataSourceProvider : ICentralDataSourceProvider
    {
        public NpgsqlDataSource Central { get; }
        public NpgsqlDataSource CentralMeta { get; }
        private bool _disposed;

        public CentralDataSourceProvider(IRadiumLocalSettings settings)
        {
            var raw = settings.CentralConnectionString;
                      

            var tracePg = Environment.GetEnvironmentVariable("RAD_TRACE_PG") == "1";

            // Primary (normal) workload builder
            var normalBuilder = new NpgsqlConnectionStringBuilder(raw)
            {
                IncludeErrorDetail = true,
                Multiplexing = false,
                KeepAlive = 15,
                NoResetOnClose = false
            };            
            if (!raw.Contains("sslmode", StringComparison.OrdinalIgnoreCase)) normalBuilder.SslMode = SslMode.Require;
            if (normalBuilder.Timeout < 8) normalBuilder.Timeout = 8;          // connect timeout
            if (normalBuilder.CommandTimeout < 30) normalBuilder.CommandTimeout = 30; // default command timeout
            if (!normalBuilder.ContainsKey("Cancellation Timeout")) normalBuilder["Cancellation Timeout"] = 4000; // ms

            // Metadata / detection builder (shorter command timeout so hung metadata queries cancel quickly)
            var metaBuilder = new NpgsqlConnectionStringBuilder(normalBuilder.ConnectionString)
            {
                CommandTimeout = 10 // fast fail for information_schema / existence checks
            };
            if (!metaBuilder.ContainsKey("Cancellation Timeout")) metaBuilder["Cancellation Timeout"] = 3000;

            var dsBuilder = new NpgsqlDataSourceBuilder(normalBuilder.ConnectionString);
            if (tracePg)
            {
                dsBuilder.UseLoggerFactory(new NpgsqlTraceLoggerFactory());
            }
            Central = dsBuilder.Build();

            var dsMetaBuilder = new NpgsqlDataSourceBuilder(metaBuilder.ConnectionString);
            if (tracePg)
            {
                dsMetaBuilder.UseLoggerFactory(new NpgsqlTraceLoggerFactory());
            }
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
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true; // we already gate by env
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
