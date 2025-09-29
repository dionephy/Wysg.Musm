using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    internal static class PgConnectionHelper
    {
        private static int _wouldBlockLogged; // track first WouldBlock log only
        // Diagnostics helper to correlate and analyze transient connect failures (e.g. WouldBlock 10035)
        private static class Diag
        {
            private static int _seq;              // Monotonic attempt id
            private static int _inFlight;         // Current concurrent opens
            private static int _peak;             // Peak concurrent opens
            private static readonly Version _npgsqlVer = typeof(NpgsqlConnection).Assembly.GetName().Version ?? new Version(0,0,0,0);
            private static int _printedEnv;       // Ensure env info printed once

            internal static int Begin(NpgsqlConnection conn, string tag)
            {
                PrintEnvOnce();
                var id = Interlocked.Increment(ref _seq);
                var nowInFlight = Interlocked.Increment(ref _inFlight);
                int snapshotPeak;
                while (true)
                {
                    snapshotPeak = Volatile.Read(ref _peak);
                    if (nowInFlight <= snapshotPeak) break;
                    if (Interlocked.CompareExchange(ref _peak, nowInFlight, snapshotPeak) == snapshotPeak) break;
                }
                var b = SafeBuilder(conn);
                Debug.WriteLine($"[PG][Open#{id}][BEGIN][{tag}] T={Environment.CurrentManagedThreadId} Tick={Environment.TickCount} InFlight={nowInFlight} Peak={Volatile.Read(ref _peak)} Host={b?.Host} Port={b?.Port} DB={b?.Database} User={b?.Username} Pooling={b?.Pooling} SSLMode={b?.SslMode} Timeout={b?.Timeout}s CmdTimeout={b?.CommandTimeout}s NoReset={b?.NoResetOnClose} Multiplexing={b?.Multiplexing}");
                return id;
            }

            internal static void MarkRetry(int id, string reason, int delayMs)
            {
                if (reason.StartsWith("WouldBlock") && Interlocked.CompareExchange(ref _wouldBlockLogged, 1, 0) != 0)
                    return; // only first WouldBlock line
                Debug.WriteLine($"[PG][Open#{id}][RETRY] Reason={reason} Delay={delayMs}ms T={Environment.CurrentManagedThreadId} Tick={Environment.TickCount}");
            }

            internal static void End(int id, bool success, Exception? ex, long elapsedMs)
            {
                var inFlight = Interlocked.Decrement(ref _inFlight);
                if (success)
                {
                    Debug.WriteLine($"[PG][Open#{id}][END][OK] Elapsed={elapsedMs}ms RemainingInFlight={inFlight} Tick={Environment.TickCount}");
                }
                else
                {
                    Debug.WriteLine($"[PG][Open#{id}][END][FAIL] Elapsed={elapsedMs}ms RemainingInFlight={inFlight} Tick={Environment.TickCount}");
                    if (ex != null)
                    {
                        DumpExceptionChain(id, ex);
                    }
                }
            }

            private static void DumpExceptionChain(int id, Exception ex)
            {
                int depth = 0;
                for (var cur = ex; cur != null; cur = cur.InnerException, depth++)
                {
                    if (cur is SocketException se)
                    {
                        Debug.WriteLine($"[PG][Open#{id}][EX][Sock depth={depth}] Code={se.SocketErrorCode} Int={(int)se.SocketErrorCode} Msg={se.Message}");
                    }
                    else
                    {
                        Debug.WriteLine($"[PG][Open#{id}][EX][depth={depth}] {cur.GetType().Name}: {cur.Message}");
                    }
                }
                Debug.WriteLine($"[PG][Open#{id}][EX][Full] {ex}");
            }

            private static NpgsqlConnectionStringBuilder? SafeBuilder(NpgsqlConnection conn)
            {
                try { return new NpgsqlConnectionStringBuilder(conn.ConnectionString); } catch { return null; }
            }

            private static void PrintEnvOnce()
            {
                if (Interlocked.Exchange(ref _printedEnv, 1) != 0) return;
                Debug.WriteLine($"[PG][Env] NpgsqlVersion={_npgsqlVer} OS={Environment.OSVersion} 64Bit={Environment.Is64BitProcess} Framework={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
                Debug.WriteLine($"[PG][Env] Proc={Environment.ProcessId} Machine={Environment.MachineName} IPv6={Socket.OSSupportsIPv6} IPv4={Socket.OSSupportsIPv4}");
            }
        }

        public static async Task OpenWithLocalSslFallbackAsync(NpgsqlConnection conn)
        {
            var sw = Stopwatch.StartNew();
            int id = Diag.Begin(conn, "Primary");
            Exception? last = null;
            bool success = false;
            try
            {
                try
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    success = true;
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (ShouldRetryWouldBlock(ex))
                    {
                        const int delay = 120;
                        Diag.MarkRetry(id, "WouldBlock(10035)", delay);
                        await Task.Delay(delay).ConfigureAwait(false);
                        try
                        {
                            await conn.OpenAsync().ConfigureAwait(false);
                            success = true; return;
                        }
                        catch (Exception ex2) { last = ex2; }
                    }
                    else if (ShouldTryLocalFallback(conn, ex, out var builder))
                    {
                        try
                        {
                            var originalMode = builder.SslMode;
                            builder.SslMode = SslMode.Prefer;
                            conn.ConnectionString = builder.ConnectionString;
                            Diag.MarkRetry(id, $"LocalFallback({originalMode}->Prefer)", 0);
                            await conn.OpenAsync().ConfigureAwait(false);
                            success = true; return;
                        }
                        catch (Exception ex2)
                        {
                            last = ex2;
                            if (ShouldRetryWouldBlock(ex2))
                            {
                                const int delay = 120;
                                Diag.MarkRetry(id, "WouldBlock(10035)AfterPrefer", delay);
                                await Task.Delay(delay).ConfigureAwait(false);
                                try
                                {
                                    await conn.OpenAsync().ConfigureAwait(false);
                                    success = true; return;
                                }
                                catch (Exception ex3) { last = ex3; }
                            }
                        }
                    }
                }
                Debug.WriteLine($"[PG][Open#{id}][FAIL] Exhausted retries: {last?.GetType().Name} {last?.Message}");
                throw last!;
            }
            finally
            {
                sw.Stop();
                Diag.End(id, success, success ? null : last, sw.ElapsedMilliseconds);
            }
        }

        private static bool ShouldRetryWouldBlock(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
                if (cur is SocketException se && se.SocketErrorCode == SocketError.WouldBlock) return true;
            return false;
        }

        private static bool ShouldTryLocalFallback(NpgsqlConnection conn, Exception ex, out NpgsqlConnectionStringBuilder b)
        {
            b = new NpgsqlConnectionStringBuilder(conn.ConnectionString);
            if (!IsLocalHost(b.Host)) return false;
            if (b.SslMode != SslMode.Require) return false;
            return IsAbortedOrTlsHandshake(ex);
        }

        private static bool IsLocalHost(string host)
            => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "::1";

        private static bool IsAbortedOrTlsHandshake(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is SocketException se && (se.SocketErrorCode == SocketError.OperationAborted || se.SocketErrorCode == SocketError.ConnectionAborted)) return true;
                if (cur is IOException io && io.Message.IndexOf("TLS", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
