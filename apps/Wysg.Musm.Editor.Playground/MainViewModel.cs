using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Editor.Playground.Completion;
using Wysg.Musm.Editor.Playground.Net;

namespace Wysg.Musm.Editor.Playground
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly GhostClient _ghost;
        private readonly DispatcherTimer _idleTimer;
        private CancellationTokenSource? _cts;

        public EditorControl Editor { get; }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        private string _status = "Ready";

        public string ReportText
        {
            get => _reportText;
            set { _reportText = value; OnPropertyChanged(); }
        }
        private string _reportText = "";

        public string PatientSex { get; set; } = "M";
        public int PatientAge { get; set; } = 68;

        public MainViewModel(EditorControl editor)
        {
            Editor = editor;

            // HTTP only (Step 0)
            _ghost = new GhostClient("http://localhost:5000/");

            // Idle timer
            _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _idleTimer.Tick += async (_, __) => { _idleTimer.Stop(); await OnIdleAsync(); };

            // OPTIONAL: seed a tiny completion list to prove the popup
            Editor.SnippetProvider = new WordListCompletionProvider(new[]
            {
                "thalamus", "no acute intracranial abnormality", "diffuse brain atrophy", "microangiopathy"
            });
        }

        public void OnUserActivity()
        {
            // Sync DocumentText mirror for the request
            ReportText = Editor.DocumentText;

            _idleTimer.Stop();
            _idleTimer.Start();
        }

        private async Task OnIdleAsync()
        {
            if (Editor.IsInPlaceholderMode) return;

            // Close popup on idle to avoid visual overlap
            if (Editor.IsCompletionWindowOpen)
                Editor.DismissCompletionPopup();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var req = new GhostClient.SuggestRequest(
                ReportText,
                PatientSex,
                PatientAge,
                new GhostClient.StudyHeader("headache", "2024-11-01"),
                new GhostClient.StudyInfo(new[] { "Ax FLAIR", "DWI", "SWI" }, "2025-09-09"),
                3, 1, 800,
                "Clinical information: headache Brain MRI w/wo contrast 2025-09-09"
            );

            try
            {
                Debug.WriteLine($"[Playground] calling GhostApi… len={ReportText?.Length ?? 0}");
                var sw = Stopwatch.StartNew();
                var res = await _ghost.SuggestAsync(req, ct);
                sw.Stop();

                if (res is null)
                {
                    Status = "GhostApi: no response";
                    return;
                }

                Status = $"GhostApi: {sw.ElapsedMilliseconds} ms · {res.Lines.Count} lines";

                var ghosts = res.Lines
                  .OrderBy(x => x.LineIndex)
                  .Select(x => (x.LineIndex, x.Ghost))
                  .ToList();

                System.Diagnostics.Debug.WriteLine(
                    "[Playground] ghosts: " + string.Join(", ",
                        ghosts.Select(g => $"[{g.LineIndex}:{(g.Ghost.Length > 18 ? g.Ghost.Substring(0, 18) + "…" : g.Ghost)}]")));

                Status = $"GhostApi: {sw.ElapsedMilliseconds} ms · {res.Lines.Count} lines · idx=[{string.Join(",", ghosts.Select(g => g.Item1))}]";

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Editor.UpdateServerGhosts(ghosts);   // ← replaces Set(...) + selection reset
                    Status = $"GhostApi: {sw.ElapsedMilliseconds} ms · {ghosts.Count} lines · idx=[{string.Join(",", ghosts.Select(g => g.LineIndex))}]";
                });

            }
            catch (TaskCanceledException) { /* user typed again */ }
            catch (Exception ex)
            {
                Status = $"GhostApi ERROR: {ex.Message}";
            }
        }

        // Force call via the top button
        public async Task ForceServerGhostsAsync()
        {
            _idleTimer.Stop();
            var req = new GhostClient.SuggestRequest(
                Editor.DocumentText, PatientSex, PatientAge,
                new GhostClient.StudyHeader("headache", "2024-11-01"),
                new GhostClient.StudyInfo(new[] { "Ax FLAIR", "DWI", "SWI" }, "2025-09-09"),
                3, 1, 800, "Clinical information: headache Brain MRI w/wo contrast 2025-09-09");
            var res = await _ghost.SuggestAsync(req, CancellationToken.None);
            var ghosts = res!.Lines.OrderBy(x => x.LineIndex).Select(x => (x.LineIndex, x.Ghost));



            Editor.ServerGhosts.Set(ghosts);
            Status = $"Forced: {res!.Lines.Count} lines";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
