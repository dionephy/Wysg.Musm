using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using Wysg.Musm.Editor.Ghosting;
using Wysg.Musm.Editor.Ui;
using ICSharpCode.AvalonEdit.Rendering; // for KnownLayer
using System.Text.RegularExpressions;
using Wysg.Musm.Editor.Snippets;



namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        private DispatcherTimer _idleTimer;
        private CancellationTokenSource? _ghostCts;
        private List<(int lineNumber, string text)> _ghosts = new();
        private int _selectedGhostIndex = -1;
        private bool _acceptingGhost;
        private MultiLineGhostRenderer _multiRenderer = null!;
        private bool _placeholderActive;


        private void InitServerGhosts()
        {
            _idleTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(2)
            };



            _idleTimer.Tick += OnIdleTimerTick;
            ResetIdleTimer();

            Editor.TextArea.PreviewKeyDown += OnPreviewKeyDownForGhosts;
            Editor.TextArea.TextEntered += (_, __) => ResetIdleTimer();
            Editor.TextChanged += (_, __) => { if (!_acceptingGhost) ClearGhosts(); ResetIdleTimer(); };

            _multiRenderer = new MultiLineGhostRenderer(
                Editor.TextArea.TextView,
                Editor.TextArea,
                () => _ghosts,
                () => _selectedGhostIndex,
                new System.Windows.Media.Typeface(Editor.FontFamily, Editor.FontStyle, Editor.FontWeight, Editor.FontStretch),
                () => Editor.FontSize);

            PlaceholderModeManager.PlaceholderModeEntered += (_, __) =>
            {
                _placeholderActive = true;
                _idleTimer.Stop();
                ClearGhosts();
            };

            PlaceholderModeManager.PlaceholderModeExited += (_, __) =>
            {
                _placeholderActive = false;
                ResetIdleTimer(); // rule: start 2s countdown after placeholder mode exits
            };
        }


        private void CleanupServerGhosts()
        {
            _idleTimer.Stop();
            _idleTimer.Tick -= OnIdleTimerTick;
            Editor.TextArea.PreviewKeyDown -= OnPreviewKeyDownForGhosts;
            _multiRenderer?.Dispose();
            Interlocked.Exchange(ref _ghostCts, null)?.Cancel();
        }

        private bool GhostsActive => _ghosts.Count > 0;

        private void ResetIdleTimer()
        {
            if (GhostsActive) return;           // ← pause while ghosts are showing
            _idleTimer.Stop();
            _idleTimer.Start();
        }

        private void OnIdleTimerTick(object? s, EventArgs e)
        {
            _idleTimer.Stop();

            // RULES: do not show multi-line ghosts when completion has a selection
            // or while snippet placeholder mode is active
            if (_placeholderActive) return;
            if (_squelchServerIdleWhilePopupSelected) return;

            // Close popup if any (your original rule #5)
            if (_completionWindow != null)
            {
                _completionWindow.Close();
                _completionWindow = null;
            }
            _ = RequestGhostsAsync();
        }


        private async System.Threading.Tasks.Task RequestGhostsAsync()
        {
            if (GhostClient is null) return;

            var cts = new CancellationTokenSource();
            var old = Interlocked.Exchange(ref _ghostCts, cts);
            old?.Cancel(); old?.Dispose();

            var req = new GhostRequest(
                ReportText: Editor.Document.Text,
                PatientSex: PatientSex ?? "U",
                PatientAge: PatientAge,
                StudyHeader: StudyHeader ?? "",
                StudyInfo: StudyInfo ?? ""
            );

            GhostResponse resp;
            try { resp = await GhostClient.SuggestAsync(req, cts.Token); }
            catch { return; }



            var doc = Editor.Document;
            var list = new List<(int lineNumber, string text)>();

            foreach (var s in resp.Suggestions ?? Enumerable.Empty<GhostSuggestion>())
            {
                int ln = Math.Max(1, Math.Min(s.LineNumber, doc.LineCount));
                var line = doc.GetLineByNumber(ln);
                var original = doc.GetText(line);

                var sug = s.Suggestion ?? string.Empty;
                if (IsWeakSuggestion(original, sug)) continue;   // ← drop weak ones

                list.Add((ln, NormalizeGhostLine(sug)));
            }

            _ghosts = list;
            _selectedGhostIndex = _ghosts.Count > 0 ? 0 : -1;

            // pause timer & repaint (as in section A.2)
            _idleTimer.Stop();
            var tv = Editor.TextArea.TextView;
            tv.Dispatcher.InvokeAsync(() =>
            {
                try { tv.EnsureVisualLines(); } catch { }
                tv.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
            }, System.Windows.Threading.DispatcherPriority.Background);



            Editor.TextArea.TextView.InvalidateVisual();
        }

        private static string NormalizeGhostLine(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            if (!s.EndsWith(".")) s += ".";
            return " " + s;
        }

        private void ClearGhosts()
        {
            if (_ghosts.Count == 0 && _selectedGhostIndex < 0) return;
            _ghosts.Clear();
            _selectedGhostIndex = -1;

            var tv = Editor.TextArea.TextView;
            tv.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);

            ResetIdleTimer();   // ← resume 2s idle
        }


        // Controls/EditorControl.ServerGhosts.cs
        private void OnPreviewKeyDownForGhosts(object? sender, KeyEventArgs e)
        {
            // If no ghosts → do nothing (let popup handler and text editor work)
            if (_ghosts.Count == 0) return;

            switch (e.Key)
            {
                case Key.Up:
                    e.Handled = true;
                    _selectedGhostIndex = Math.Max(0, _selectedGhostIndex - 1);
                    Editor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
                    return;

                case Key.Down:
                    e.Handled = true;
                    _selectedGhostIndex = Math.Min(_ghosts.Count - 1, Math.Max(0, _selectedGhostIndex + 1));
                    Editor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
                    return;

                case Key.Escape:
                    e.Handled = true;
                    ClearGhosts();
                    return;

                case Key.Tab:
                    e.Handled = true;
                    AcceptSelectedGhost();
                    return;

                // IMPORTANT: do not set e.Handled for any other key
                default:
                    return;
            }
        }



        private void AcceptSelectedGhost()
        {
            if (_selectedGhostIndex < 0 || _selectedGhostIndex >= _ghosts.Count) return;

            var (lineNumber, text) = _ghosts[_selectedGhostIndex];
            var docLine = Editor.Document.GetLineByNumber(lineNumber);

            _acceptingGhost = true;
            try
            {
                Editor.Document.Replace(docLine.Offset, docLine.Length, (text ?? string.Empty).Trim());
            }
            finally { _acceptingGhost = false; }

            _ghosts.RemoveAt(_selectedGhostIndex);
            _selectedGhostIndex = _ghosts.Count == 0 ? -1 : Math.Min(_selectedGhostIndex, _ghosts.Count - 1);

            var tv = Editor.TextArea.TextView;
            tv.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);

            if (_ghosts.Count == 0) ResetIdleTimer();   // ← resume when empty
        }

        private static string NormalizeForCompare(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.ToLowerInvariant().Trim();
            s = Regex.Replace(s, @"[\s\p{P}]+", " "); // collapse punctuation/whitespace
            return s;
        }

        private static bool IsWeakSuggestion(string originalLine, string suggestion)
        {
            var a = NormalizeForCompare(originalLine);
            var b = NormalizeForCompare(suggestion);

            if (string.IsNullOrWhiteSpace(b)) return true;     // empty/whitespace
            if (b.EndsWith(" ...")) return true;               // fake fallback from playground
            if (b.Length < 6) return true;                     // too short to be useful
            if (a == b) return true;                           // identical to original
            if (b.StartsWith(a) && (b.Length - a.Length) < 4)  // tiny tail append
                return true;

            return false;
        }

    }
}
