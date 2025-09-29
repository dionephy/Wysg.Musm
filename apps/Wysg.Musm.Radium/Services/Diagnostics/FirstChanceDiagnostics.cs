using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Wysg.Musm.Radium.Services.Diagnostics
{
    internal static class FirstChanceDiagnostics
    {
        private static int _oceLogged;          // number of OCE logs
        private static int _sslOnce;            // first SSL cancel flag
        private static int _sockLogged;         // number of SocketException logs (staged)
        private static readonly object _fileLock = new();

        public static void Initialize()
        {
            AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
            {
                if (Environment.GetEnvironmentVariable("RAD_SUPPRESS_OCE") == "1" && e.Exception is OperationCanceledException) return;
                if (Environment.GetEnvironmentVariable("RAD_SUPPRESS_SOCKET") == "1" && e.Exception is System.Net.Sockets.SocketException) return;

                if (e.Exception is OperationCanceledException oce)
                {
                    var st = oce.StackTrace ?? string.Empty;
                    if (string.IsNullOrEmpty(st) || !st.Contains('\n')) return;
                    if (st.Contains("SslStream"))
                    {
                        if (Interlocked.CompareExchange(ref _sslOnce, 1, 0) == 0)
                            Debug.WriteLine("[Diag][OCE][SSL] TLS cancel (only once)");
                        return;
                    }
                    if (st.Contains("System.Net.Sockets") || st.Contains("System.Threading.Tasks.Sources") || st.Contains("CancellationToken")) return;
                    if (Volatile.Read(ref _oceLogged) < 2)
                    {
                        var idx = Interlocked.Increment(ref _oceLogged);
                        Debug.WriteLine($"[Diag][OCE#{idx}] {oce.Message}\n{st}");
                    }
                    return;
                }
                else if (e.Exception is System.Net.Sockets.SocketException se)
                {
                    // Extra suppression list (comma-separated SocketError names): RAD_SOCKET_SUPPRESS_CODES=TimedOut,ConnectionReset
                    var suppressList = Environment.GetEnvironmentVariable("RAD_SOCKET_SUPPRESS_CODES");
                    if (!string.IsNullOrWhiteSpace(suppressList))
                    {
                        foreach (var token in suppressList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            if (Enum.TryParse<System.Net.Sockets.SocketError>(token, true, out var sup) && sup == se.SocketErrorCode)
                                return; // suppressed by user list
                        }
                    }

                    if (se.SocketErrorCode == System.Net.Sockets.SocketError.WouldBlock ||
                        se.SocketErrorCode == System.Net.Sockets.SocketError.OperationAborted)
                        return;

                    if (se.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut ||
                        se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset ||
                        se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionAborted ||
                        se.SocketErrorCode == System.Net.Sockets.SocketError.Shutdown ||
                        se.SocketErrorCode == System.Net.Sockets.SocketError.HostUnreachable ||
                        se.SocketErrorCode == System.Net.Sockets.SocketError.NetworkDown)
                    {
                        int n = Interlocked.Increment(ref _sockLogged);
                        if (n <= 2)
                        {
                            var st = se.StackTrace ?? string.Empty;
                            var line = $"[Diag][SOCK#{n}] Code={se.SocketErrorCode} Int={(int)se.SocketErrorCode} Msg={se.Message}";
                            Debug.WriteLine(line);
                            if (!string.IsNullOrEmpty(st)) Debug.WriteLine(st);
                            try
                            {
                                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wysg.Musm");
                                Directory.CreateDirectory(dir);
                                var path = Path.Combine(dir, "diag_socket.log");
                                lock (_fileLock)
                                {
                                    File.AppendAllText(path, $"===== SOCKET #{n} {DateTime.UtcNow:O} ====={Environment.NewLine}{line}{Environment.NewLine}{st}{Environment.NewLine}{Environment.NewLine}");
                                }
                            }
                            catch { }
                        }
                        return;
                    }
                }
            };
        }
    }

    internal static class BackgroundTask
    {
        public static void Run(string name, Func<System.Threading.Tasks.Task> work)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try { await work().ConfigureAwait(false); }
                catch (OperationCanceledException) { Debug.WriteLine($"[BG][{name}] canceled"); }
                catch (Exception ex) { Debug.WriteLine($"[BG][{name}][EX] {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}"); }
            });
        }
    }
}
