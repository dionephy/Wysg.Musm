using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document; // TextSegment, ISegment
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wysg.Musm.Editor.Completion;      // WordBoundaryHelper
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Editor.Ui;

namespace Wysg.Musm.Editor.Controls
{
    /// <summary>
    /// Musm editor shell that orchestrates: ghost text + completion popup (coexist mode).
    /// </summary>
    public partial class EditorControl : UserControl
    {
        // ===== Dependency Properties =====
        public static readonly DependencyProperty DocumentTextProperty =
            DependencyProperty.Register(nameof(DocumentText), typeof(string), typeof(EditorControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty AiEnabledProperty =
            DependencyProperty.Register(nameof(AiEnabled), typeof(bool), typeof(EditorControl),
                new PropertyMetadata(true, OnAiEnabledChanged));

        public static readonly DependencyProperty CompletionEngineProperty =
            DependencyProperty.Register(nameof(CompletionEngine), typeof(ICompletionEngine), typeof(EditorControl),
                new PropertyMetadata(NullCompletionEngine.Instance));

        public static readonly DependencyProperty SnippetProviderProperty =
            DependencyProperty.Register(nameof(SnippetProvider), typeof(ISnippetProvider), typeof(EditorControl),
                new PropertyMetadata(NullSnippetProvider.Instance));

        public static readonly DependencyProperty DebounceMsProperty =
            DependencyProperty.Register(nameof(DebounceMs), typeof(int), typeof(EditorControl),
                new PropertyMetadata(200, OnDebounceMsChanged));

        /// <summary>Auto popup while typing (prefix-length based).</summary>
        public static readonly DependencyProperty AutoSuggestOnTypingProperty =
            DependencyProperty.Register(nameof(AutoSuggestOnTyping), typeof(bool), typeof(EditorControl),
                new PropertyMetadata(true));

        /// <summary>Minimum letters in the current word before auto popup.</summary>
        public static readonly DependencyProperty MinCharsForSuggestProperty =
            DependencyProperty.Register(nameof(MinCharsForSuggest), typeof(int), typeof(EditorControl),
                new PropertyMetadata(2));

        public string DocumentText { get => (string)GetValue(DocumentTextProperty); set => SetValue(DocumentTextProperty, value); }
        public bool AiEnabled { get => (bool)GetValue(AiEnabledProperty); set => SetValue(AiEnabledProperty, value); }
        public ICompletionEngine CompletionEngine { get => (ICompletionEngine)GetValue(CompletionEngineProperty); set => SetValue(CompletionEngineProperty, value); }
        public ISnippetProvider SnippetProvider { get => (ISnippetProvider)GetValue(SnippetProviderProperty); set => SetValue(SnippetProviderProperty, value); }
        public int DebounceMs { get => (int)GetValue(DebounceMsProperty); set => SetValue(DebounceMsProperty, value); }
        public bool AutoSuggestOnTyping { get => (bool)GetValue(AutoSuggestOnTypingProperty); set => SetValue(AutoSuggestOnTypingProperty, value); }
        public int MinCharsForSuggest { get => (int)GetValue(MinCharsForSuggestProperty); set => SetValue(MinCharsForSuggestProperty, value); }

        // ===== Commands =====
        public ICommand ForceSuggestCommand { get; } = new RoutedUICommand("ForceSuggest", "ForceSuggest", typeof(EditorControl));
        public ICommand AcceptGhostCommand { get; } = new RoutedUICommand("AcceptGhost", "AcceptGhost", typeof(EditorControl));
        public ICommand CancelGhostCommand { get; } = new RoutedUICommand("CancelGhost", "CancelGhost", typeof(EditorControl));

        // ===== Internals =====
        private GhostTextRenderer _ghostRenderer = null!;
        private EditorAdapter _adapter = null!;
        private MusmCompletionWindow? _completionWindow;
        private CancellationTokenSource? _cts;

        private readonly DispatcherTimer _debounce;
        private readonly DispatcherTimer _scrollSilenceTimer;
        private bool _isScrolling;
        private string _ghostText = string.Empty;

        public EditorControl()
        {
            InitializeComponent();

            // Editor hooks
            Editor.TextArea.TextEntered += OnTextEntered;
            Editor.TextArea.TextEntering += OnTextEntering;
            Editor.TextArea.PreviewKeyDown += OnTextAreaPreviewKeyDown; // central key rules

            try { Editor.TextArea.SelectionChanged += OnSelectionChanged; } catch { /* older AvalonEdit */ }
            Editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;
            Editor.GotKeyboardFocus += OnFocusChanged;
            Editor.LostKeyboardFocus += OnFocusChanged;

            // Ghost renderer & adapter
            _ghostRenderer = new GhostTextRenderer(
                Editor.TextArea.TextView,
                Editor.TextArea,
                () => _ghostText,
                () => new Typeface(Editor.FontFamily, Editor.FontStyle, Editor.FontWeight, Editor.FontStretch),
                () => Editor.FontSize
            );
            _adapter = new EditorAdapter(Editor);

            // Debounce (UI thread)
            _debounce = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMs)
            };
            _debounce.Tick += OnDebounceTick;

            // Scroll silence
            _scrollSilenceTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _scrollSilenceTimer.Tick += (_, __) =>
            {
                _scrollSilenceTimer.Stop();
                _isScrolling = false;
                RestartDebounce();
            };

            // Change signals
            _adapter.TextChanged += (_, __) => { EvaluateAutoPopupVisibility(); RestartDebounce(); };
            _adapter.CaretMoved += (_, __) => { EvaluateAutoPopupVisibility(); RestartDebounce(); };

            // Command bindings
            CommandBindings.Add(new CommandBinding(ForceSuggestCommand, (_, __) => ShowCompletionForCurrentWord()));
            CommandBindings.Add(new CommandBinding(AcceptGhostCommand, (_, __) => AcceptGhost()));
            CommandBindings.Add(new CommandBinding(CancelGhostCommand, (_, __) => CancelGhost()));

            // Make sure Tab insertion is disabled (so Tab can accept ghost/popup)
            Loaded += (_, __) => DisableTabInsertion();

            // Clean up
            Unloaded += OnUnloaded;
        }

        // ===== Property change callbacks =====
        private static void OnDebounceMsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            self._debounce.Interval = TimeSpan.FromMilliseconds(Math.Max(0, (int)e.NewValue));
        }

        private static void OnAiEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            if (!(bool)e.NewValue)
            {
                self.SetGhost(string.Empty);
                self._debounce.Stop();
            }
            else
            {
                self.RestartDebounce();
            }
        }

        // ===== Lifecycle =====
        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _debounce.Stop();
            _debounce.Tick -= OnDebounceTick;
            _scrollSilenceTimer.Stop();

            Interlocked.Exchange(ref _cts, null)?.Cancel();
            Interlocked.Exchange(ref _cts, null)?.Dispose();

            if (_completionWindow != null)
            {
                _completionWindow.PreviewKeyDown -= OnCompletionWindowPreviewKeyDown;
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow.Close();
                _completionWindow = null;
            }

            Editor.TextArea.TextEntered -= OnTextEntered;
            Editor.TextArea.TextEntering -= OnTextEntering;
            Editor.TextArea.PreviewKeyDown -= OnTextAreaPreviewKeyDown;
            try { Editor.TextArea.SelectionChanged -= OnSelectionChanged; } catch { }
            Editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChanged;
            Editor.GotKeyboardFocus -= OnFocusChanged;
            Editor.LostKeyboardFocus -= OnFocusChanged;

            try { _ghostRenderer.Dispose(); } catch { }
        }

        // ===== Debounce / pause logic =====
        private void OnDebounceTick(object? sender, EventArgs e)
        {
            _debounce.Stop();
            if (ShouldPauseSuggestions()) return;
            _ = SuggestAsync();
        }

        private void RestartDebounce()
        {
            if (ShouldPauseSuggestions()) return;
            _debounce.Stop();
            _debounce.Start();
        }

        private bool ShouldPauseSuggestions()
        {
            if (!AiEnabled) return true;
            // Coexist mode: DO NOT pause ghost when the popup is open.
            if (!Editor.IsKeyboardFocusWithin) return true;
            if (_isScrolling) return true;

            var sel = Editor.TextArea.Selection;
            if (!sel.IsEmpty) return true;

            return false;
        }

        // ===== Ghost handling =====
        private void SetGhost(string text)
        {
            _ghostText = text ?? string.Empty;
            Editor.TextArea.TextView.InvalidateVisual();
        }

        private async System.Threading.Tasks.Task SuggestAsync()
        {
            if (ShouldPauseSuggestions()) return;

            var newCts = new CancellationTokenSource();
            var old = Interlocked.Exchange(ref _cts, newCts);
            old?.Cancel();
            old?.Dispose();
            var ct = newCts.Token;

            var (left, right) = _adapter.GetContextWindows(512, 0);
            SetGhost(string.Empty);

            var sb = new StringBuilder();
            await foreach (var chunk in CompletionEngine.StreamAsync(new CompletionRequest(left, right, 32), ct))
            {
                if (ct.IsCancellationRequested || ShouldPauseSuggestions()) break;

                sb.Append(chunk);
                var normalized = NormalizeGhostForDisplay(sb.ToString(), left);
                SetGhost(normalized);
            }

            /*
            try
            {
                var sb = new StringBuilder();
                await foreach (var chunk in CompletionEngine.StreamAsync(new CompletionRequest(left, right, 32), ct))
                {
                    if (ct.IsCancellationRequested || ShouldPauseSuggestions()) break;
                    sb.Append(chunk);
                    SetGhost(sb.ToString());
                }
            }
            catch (OperationCanceledException) { }
            catch { }
            */
        }

        private void AcceptGhost()
        {
            if (string.IsNullOrEmpty(_ghostText)) return;
            Editor.Document.Insert(Editor.CaretOffset, _ghostText);
            SetGhost(string.Empty);
        }

        private void CancelGhost() => SetGhost(string.Empty);

        // ===== Completion popup =====
        private void OnTextEntered(object? s, TextCompositionEventArgs e)
        {
            // IME-safe
            bool imeEnabled = InputMethod.GetIsInputMethodEnabled(Editor);
            bool imeOn = InputMethod.Current?.ImeState == InputMethodState.On;
            if (imeEnabled && imeOn) return;

            // Manual triggers
            if (e.Text == ";" || e.Text == ":")
            {
                ShowCompletionForCurrentWord();
                return;
            }

            // Auto popup on letters/digits with threshold
            if (AutoSuggestOnTyping && e.Text.Length == 1 && char.IsLetterOrDigit(e.Text[0]))
                TryOpenAutoPopupIfThresholdMet();

            // NEW: if popup is open and we typed a word char, clear selection AFTER filtering runs
            if (_completionWindow != null && e.Text.Length == 1 && char.IsLetterOrDigit(e.Text[0]))
                Dispatcher.BeginInvoke((Action)ClearPopupSelection, DispatcherPriority.Background);
        }

        private void OnTextEntering(object? s, TextCompositionEventArgs e)
        {
            // If popup open and next char is non-word, request insertion
            if (_completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        private void TryOpenAutoPopupIfThresholdMet()
        {
            if (_completionWindow != null) return;
            int prefixLen = GetCurrentWordPrefixLength();
            if (prefixLen < Math.Max(1, MinCharsForSuggest)) return;

            var items = SnippetProvider.GetCompletions(Editor);
            if (!items.Any()) return;

            items = SnippetProvider.GetCompletions(Editor);
            OpenPopupWithNoSelection(items);
        }

        private void ShowCompletionForCurrentWord()
        {
            if (_completionWindow != null)
            {
                _completionWindow.PreviewKeyDown -= OnCompletionWindowPreviewKeyDown;
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow.Close();
                _completionWindow = null;
            }

            var items = SnippetProvider.GetCompletions(Editor);
            if (!items.Any()) return;

            items = SnippetProvider.GetCompletions(Editor);
            OpenPopupWithNoSelection(items);
        }

        private void OpenPopupWithNoSelection(System.Collections.Generic.IEnumerable<ICompletionData> items)
        {
            _completionWindow = MusmCompletionWindow.ShowForCurrentWord(Editor, items);

            // Coexist mode: keep ghost visible (do NOT clear it here).

            ClearPopupSelection();



            /*
            // Open with NO selection so Tab defaults to ghost
            try
            {
                var list = _completionWindow.CompletionList;
                list.SelectedItem = null;
                list.ListBox?.UnselectAll();
            }
            catch { }
            */

            // Forward Home/End to the editor
            _completionWindow.PreviewKeyDown += OnCompletionWindowPreviewKeyDown;
            _completionWindow.Closed += OnCompletionClosed;
        }

        private void OnCompletionClosed(object? sender, EventArgs e)
        {
            if (_completionWindow != null)
            {
                _completionWindow.PreviewKeyDown -= OnCompletionWindowPreviewKeyDown;
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow = null;
            }
            RestartDebounce();
        }

        // Auto-close popup when prefix shrinks below threshold
        private void EvaluateAutoPopupVisibility()
        {
            if (_completionWindow == null) return;
            int prefixLen = GetCurrentWordPrefixLength();
            if (prefixLen < Math.Max(1, MinCharsForSuggest))
            {
                _completionWindow.PreviewKeyDown -= OnCompletionWindowPreviewKeyDown;
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow.Close();
                _completionWindow = null;
            }
        }

        private int GetCurrentWordPrefixLength()
        {
            var doc = Editor.Document;
            int caret = Editor.CaretOffset;
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);

            var (startLocal, _) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
            return local - startLocal;
        }

        // ===== Key routing (TextArea is the authority) =====
        private void OnTextAreaPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            bool hasPopup = _completionWindow != null;
            bool hasGhost = !string.IsNullOrEmpty(_ghostText);
            var selected = hasPopup ? _completionWindow!.CompletionList.SelectedItem as ICompletionData : null;

            if (hasPopup)
            {
                // Up/Down navigate popup
                if (e.Key == Key.Up) { MovePopupSelection(-1); e.Handled = true; return; }
                if (e.Key == Key.Down) { MovePopupSelection(+1); e.Handled = true; return; }

                // Tab: ghost wins if present; else selected item if any
                if (e.Key == Key.Tab)
                {
                    if (hasGhost)
                    {
                        e.Handled = true;
                        AcceptGhost();
                        // keep popup open to continue browsing; remove next line if you prefer to close
                        return;
                    }
                    if (selected != null)
                    {
                        e.Handled = true;
                        AcceptSelectedFromPopup(addTrailingSpace: false);
                        return;
                    }
                    // else: no selection, no ghost -> let Tab bubble (should be disabled anyway)
                }

                // Enter: accept selected (if any), else newline
                if (e.Key == Key.Enter && selected != null)
                {
                    e.Handled = true;
                    AcceptSelectedFromPopup(addTrailingSpace: false);
                    return;
                }

                // Space: accept selected + space (if any), else literal space
                if (e.Key == Key.Space && selected != null)
                {
                    e.Handled = true;
                    AcceptSelectedFromPopup(addTrailingSpace: true);
                    return;
                }

                // All other keys fall through.
            }
            else
            {
                // No popup → Tab/Esc control ghost
                if (hasGhost && e.Key == Key.Tab)
                {
                    e.Handled = true;
                    AcceptGhost();
                    return;
                }
                if (hasGhost && e.Key == Key.Escape)
                {
                    e.Handled = true;
                    CancelGhost();
                    return;
                }
            }
        }

        // Prevent ListBox from eating Home/End; forward to editor
        private void OnCompletionWindowPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (_completionWindow == null) return;

            // We centralize key handling here for cases where focus is inside the popup (ListBox).
            // This prevents the first Tab from doing focus traversal (which closes the popup).
            var list = _completionWindow.CompletionList;
            var selected = list.SelectedItem as ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData;
            bool hasGhost = !string.IsNullOrEmpty(_ghostText);

            // Up/Down still navigate in TextArea handler; do nothing here for them.
            if (e.Key == Key.Up || e.Key == Key.Down)
                return;

            // TAB: ghost wins if present; else selected item (if any)
            if (e.Key == Key.Tab)
            {
                e.Handled = true; // stop focus traversal / popup auto-close
                if (hasGhost)
                {
                    AcceptGhost();  // keep popup open by design
                }
                else if (selected != null)
                {
                    AcceptSelectedFromPopup(addTrailingSpace: false);
                }
                // If neither ghost nor selection, swallow Tab (no \t insertion).
                return;
            }

            // ENTER: accept selected (if any), else let it fall through to editor (newline)
            if (e.Key == Key.Enter)
            {
                if (selected != null)
                {
                    e.Handled = true;
                    AcceptSelectedFromPopup(addTrailingSpace: false);
                }
                return;
            }

            // SPACE: accept selected + space (if any), else let literal space go to editor
            if (e.Key == Key.Space)
            {
                if (selected != null)
                {
                    e.Handled = true;
                    AcceptSelectedFromPopup(addTrailingSpace: true);
                }
                return;
            }

            // HOME/END: act on the editor (not the popup ListBox)
            if (e.Key == Key.Home || e.Key == Key.End)
            {
                e.Handled = true;
                bool extend = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                bool wholeDoc = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                MoveCaretHomeEndOnEditor(e.Key == Key.End, extend, wholeDoc);
                return;
            }

            // Everything else: let it bubble to the editor.
        }


        // Accept selected item (optionally add trailing space)
        private void AcceptSelectedFromPopup(bool addTrailingSpace)
        {
            if (_completionWindow == null) return;
            var list = _completionWindow.CompletionList;
            var item = list.SelectedItem as ICompletionData;
            if (item == null) return; // no explicit selection → do nothing

            int start = _completionWindow.StartOffset;
            int end = _completionWindow.EndOffset;
            int length = Math.Max(0, end - start);
            ISegment segment = new TextSegment { StartOffset = start, Length = length };

            item.Complete(Editor.TextArea, segment, EventArgs.Empty);

            if (addTrailingSpace)
                Editor.Document.Insert(Editor.TextArea.Caret.Offset, " ");
        }

        private void MovePopupSelection(int delta)
        {
            if (_completionWindow == null) return;
            var list = _completionWindow.CompletionList;
            var data = list.CompletionData;
            if (data.Count == 0) return;

            int idx = -1;
            if (list.SelectedItem is ICompletionData sel)
            {
                for (int i = 0; i < data.Count; i++)
                    if (ReferenceEquals(data[i], sel)) { idx = i; break; }
            }

            idx = idx < 0 ? (delta > 0 ? 0 : data.Count - 1)
                          : Math.Clamp(idx + delta, 0, data.Count - 1);

            list.SelectedItem = data[idx];
            try { list.ListBox?.ScrollIntoView(list.SelectedItem); } catch { }
        }

        // ===== Caret helpers for Home/End and selection extension =====
        private void MoveCaretHomeEndOnEditor(bool toEnd, bool extend, bool wholeDoc)
        {
            var doc = Editor.Document;
            int caret = Editor.CaretOffset;

            int target;
            if (wholeDoc)
            {
                target = toEnd ? doc.TextLength : 0;
            }
            else
            {
                var line = doc.GetLineByOffset(caret);
                target = toEnd ? line.EndOffset : line.Offset;
            }

            ApplyCaretMoveWithSelection(target, extend);
        }

        private void ApplyCaretMoveWithSelection(int targetOffset, bool extend)
        {
            var ta = Editor.TextArea;
            int caret = ta.Caret.Offset;

            if (!extend)
            {
                Editor.SelectionLength = 0;
                Editor.CaretOffset = targetOffset;
                return;
            }

            var sel = ta.Selection;
            int anchor;
            if (sel.IsEmpty)
            {
                anchor = caret;
            }
            else
            {
                var seg = sel.SurroundingSegment; // ISegment: Offset + Length
                int segStart = seg.Offset;
                int segEnd = seg.Offset + seg.Length;
                anchor = (caret == segEnd) ? segStart : segEnd;
            }

            int start = Math.Min(anchor, targetOffset);
            int len = Math.Abs(targetOffset - anchor);

            Editor.Select(start, len);
            Editor.CaretOffset = targetOffset;
        }

        // ===== Misc =====
        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            if (ShouldPauseSuggestions()) { _debounce.Stop(); SetGhost(string.Empty); }
            else RestartDebounce();
        }

        private void OnScrollOffsetChanged(object? sender, EventArgs e)
        {
            _isScrolling = true;
            _scrollSilenceTimer.Stop();
            _scrollSilenceTimer.Start();
            _debounce.Stop();
            SetGhost(string.Empty);
        }

        private void OnFocusChanged(object? sender, RoutedEventArgs e)
        {
            if (ShouldPauseSuggestions()) { _debounce.Stop(); SetGhost(string.Empty); }
            else RestartDebounce();
        }

        private void DisableTabInsertion()
        {
            // Remove any Tab key bindings attached to the TextArea
            var toRemove = Editor.TextArea.InputBindings
                .OfType<KeyBinding>()
                .Where(kb => kb.Key == Key.Tab)
                .ToList();
            foreach (var kb in toRemove)
                Editor.TextArea.InputBindings.Remove(kb);

            // Swallow TabForward / TabBackward editing commands (indent/outdent)
            void Exec(object s, ExecutedRoutedEventArgs e) { e.Handled = true; }
            void Can(object s, CanExecuteRoutedEventArgs e) { e.CanExecute = true; e.Handled = true; }

            Editor.TextArea.CommandBindings.Add(new CommandBinding(EditingCommands.TabForward, Exec, Can));
            Editor.TextArea.CommandBindings.Add(new CommandBinding(EditingCommands.TabBackward, Exec, Can));
        }

        // ===== Nulls =====
        private sealed class NullCompletionEngine : ICompletionEngine
        {
            public static readonly NullCompletionEngine Instance = new();
            public async IAsyncEnumerable<string> StreamAsync(CompletionRequest req, System.Threading.CancellationToken ct)
            {
                await System.Threading.Tasks.Task.CompletedTask;
                yield break;
            }
        }

        private sealed class NullSnippetProvider : ISnippetProvider
        {
            public static readonly NullSnippetProvider Instance = new();
            public System.Collections.Generic.IEnumerable<ICompletionData>
                GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
                => Array.Empty<ICompletionData>();
        }

        private void ClearPopupSelection()
        {
            if (_completionWindow == null) return;
            var list = _completionWindow.CompletionList;
            list.SelectedItem = null;
            try { list.ListBox?.UnselectAll(); } catch { /* best effort */ }
        }

        // Treat letters/digits/_/- as word chars (keep consistent with your boundary helper)
        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-';

        /// Extract last word prefix from the absolute left context (whole doc left of caret),
        /// and tell whether the caret was after whitespace (e.g., "no ").
        private static (string prefix, bool endsWithSpace) GetPrefixAndSpaceFromLeft(string left)
        {
            if (string.IsNullOrEmpty(left)) return (string.Empty, false);

            int i = left.Length - 1;
            bool endsWithSpace = false;

            // skip trailing whitespace right before caret
            while (i >= 0 && char.IsWhiteSpace(left[i])) { i--; endsWithSpace = true; }
            if (i < 0) return (string.Empty, endsWithSpace);

            int end = i;
            while (i >= 0 && IsWordChar(left[i])) i--;
            int start = i + 1;

            if (start > end) return (string.Empty, endsWithSpace);
            return (left.Substring(start, end - start + 1), endsWithSpace);
        }

        /// Longest case-insensitive overlap between end of 'prefix' and start of 'ghost'.
        private static int ComputeBoundaryOverlap(string prefix, string ghost)
        {
            int max = System.Math.Min(prefix.Length, ghost.Length);
            for (int k = max; k > 0; k--)
            {
                // suffix of prefix vs prefix of ghost
                if (prefix[^k..].Equals(ghost[..k], System.StringComparison.OrdinalIgnoreCase))
                    return k;
            }
            return 0;
        }

        /// Normalize the ghost so it never repeats the last typed char(s) at the boundary
        /// and never shows a duplicated leading space after the user already typed one.
        private string NormalizeGhostForDisplay(string rawGhost, string fullLeftContext)
        {
            if (string.IsNullOrEmpty(rawGhost)) return string.Empty;

            var (prefix, endsWithSpace) = GetPrefixAndSpaceFromLeft(fullLeftContext);

            // If caret already sits after a space (typed "no "), drop one leading space from ghost.
            string g = rawGhost;
            if (endsWithSpace && g.Length > 0 && g[0] == ' ')
                g = g.Substring(1);

            // Remove duplicated boundary letters: if prefix ends with 'l' and ghost starts with 'l', drop it.
            int overlap = ComputeBoundaryOverlap(prefix, g);
            if (overlap > 0 && overlap <= g.Length)
                g = g.Substring(overlap);

            // Ensure a trailing space for smooth typing (optional; keep your UX)
            if (!g.EndsWith(" "))
                g += " ";

            return g;
        }

    }
}
