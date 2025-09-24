using System;
using System.Collections.Concurrent;
using Npgsql;
using Serilog;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Lightweight first-chance Postgres exception sampler to reduce debugger noise and surface root causes.
    /// Logs only the first occurrence of a distinct (SqlState|MessageText) pair (now enriched with table/schema/position/inner message).
    /// </summary>
    public static class PgDebug
    {
        private static bool _initialized;
        private static readonly ConcurrentDictionary<string, byte> _seen = new();

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
            Log.Information("[PgDebug] Initialized first-chance Postgres exception sampler");
        }

        private static void OnFirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is PostgresException pex)
            {
                try
                {
                    var key = pex.SqlState + "|" + pex.MessageText;
                    if (_seen.TryAdd(key, 1))
                    {
                        var innerMsg = pex.InnerException?.Message;
                        Log.Debug("[PgDebug][FirstChance] SqlState={SqlState} Msg={Msg} Detail={Detail} Hint={Hint} Schema={Schema} Table={Table} Position={Position} Where={Where} Inner={Inner}",
                            pex.SqlState,
                            pex.MessageText?.Trim(),
                            pex.Detail,
                            pex.Hint,
                            pex.SchemaName,
                            pex.TableName,
                            pex.Position,
                            pex.Where,
                            innerMsg);
                    }
                }
                catch { }
            }
        }
    }
}
