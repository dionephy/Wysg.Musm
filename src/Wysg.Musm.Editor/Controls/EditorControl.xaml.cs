using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Editor.Ui;

namespace Wysg.Musm.Editor.Controls
{
    /// <summary>
    /// Orchestrates ghost text + snippet popup over a MusmEditor.
    /// </summary>
    public partial class EditorControl : UserControl
    {
        // ===== Dependency Properties (bind from ViewModel) =====
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

        public string DocumentText
        {
            get => (string)GetValue(DocumentTextProperty);
            set => SetValue(DocumentTextProperty, value);
        }

        public bool AiEnabled
        {
            get => (bool)GetValue(AiEnabledProperty);
            set => SetValue(AiEnabledProperty, value);
        }

        public ICompletionEngine CompletionEngine
        {
            get => (ICompletionEngine)GetValue(CompletionEngineProperty);
            set => SetValue(CompletionEngineProperty, value);
        }

        public ISnippetProvider SnippetProvider
        {
            get => (ISnippetProvider)GetValue(SnippetProviderProperty);
            set => SetValue(SnippetProviderProperty, value);
        }

        /// <summary>Debounce interval for ghost suggestions (ms).</summary>
        public int DebounceMs
        {
            get => (int)GetValue(DebounceMsProperty);
            set => SetValue(DebounceMsProperty, value);
        }

        // Commands (host can bind to these too if desired)
        public ICommand ForceSuggestCommand { get; } = new RoutedUICommand("ForceSuggest", "ForceSuggest", typeof(EditorControl));
        public ICommand AcceptGhostCommand { get; } = new RoutedUICommand("AcceptGhost", "AcceptGhost", typeof(EditorControl));
        public ICommand CancelGhostCommand { get; } = new RoutedUICommand("CancelGhost", "CancelGhost", typeof(EditorControl));

        // ===== Internals (view-only behavior) =====
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

            // Selection changes pause ghost suggestions
            try { Editor.TextArea.SelectionChanged += OnSelectionChanged; } catch { /* older AvalonEdit */ }

            // Pause during scroll (resume after short silence)
            Editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;

            // Focus sensitivity
            Editor.GotKeyboardFocus += OnFocusChanged;
            Editor.LostKeyboardFocus += OnFocusChanged;

            // Ghost renderer
            _ghostRenderer = new GhostTextRenderer(Editor.TextArea.TextView, () => _ghostText);
            _adapter = new EditorAdapter(Editor);

            // UI-thread debounce for suggestions
            _debounce = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMs)
            };
            _debounce.Tick += OnDebounceTick;

            // Scroll “silence” timer (short pause after scroll stops)
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

            // Low-level change signals → restart debounce
            _adapter.TextChanged += (_, __) => RestartDebounce();
            _adapter.CaretMoved += (_, __) => RestartDebounce();

            // Command bindings
            CommandBindings.Add(new CommandBinding(ForceSuggestCommand, (_, __) => ShowSnippetCompletion()));
            CommandBindings.Add(new CommandBinding(AcceptGhostCommand, (_, __) => AcceptGhost()));
            CommandBindings.Add(new CommandBinding(CancelGhostCommand, (_, __) => CancelGhost()));

            // Clean up on unload
            Unloaded += OnUnloaded;
        }

        // ========== Property change callbacks ==========
        private static void OnDebounceMsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            var ms = Math.Max(0, (int)e.NewValue);
            self._debounce.Interval = TimeSpan.FromMilliseconds(ms);
        }

        private static void OnAiEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            if (!(bool)e.NewValue)
            {
                // Clear ghost immediately when AI is toggled off
                self.SetGhost(string.Empty);
                self._debounce.Stop();
            }
            else
            {
                self.RestartDebounce();
            }
        }

        // ========== Lifecycle cleanup ==========
        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _debounce.Stop();
            _debounce.Tick -= OnDebounceTick;

            _scrollSilenceTimer.Stop();
            _scrollSilenceTimer.Tick -= (_, __) => { }; // already inline; harmless

            Interlocked.Exchange(ref _cts, null)?.Cancel();
            Interlocked.Exchange(ref _cts, null)?.Dispose();

            if (_completionWindow != null)
            {
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow.Close();
                _completionWindow = null;
            }

            // Unhook editor events
            Editor.TextArea.TextEntered -= OnTextEntered;
            Editor.TextArea.TextEntering -= OnTextEntering;
            try { Editor.TextArea.SelectionChanged -= OnSelectionChanged; } catch { /* older AvalonEdit */ }
            Editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChanged;
            Editor.GotKeyboardFocus -= OnFocusChanged;
            Editor.LostKeyboardFocus -= OnFocusChanged;

            // Detach ghost renderer to avoid leaks
            try { _ghostRenderer.Dispose(); } catch { }
        }

        // ========== Debounce / pause logic ==========
        private void OnDebounceTick(object? sender, EventArgs e)
        {
            _debounce.Stop();

            if (ShouldPauseSuggestions())
                return;

            _ = SuggestAsync(); // fire-and-forget; stays on UI thread unless awaited internally
        }

        private void RestartDebounce()
        {
            if (ShouldPauseSuggestions())
                return;

            _debounce.Stop();
            _debounce.Start();
        }

        private bool ShouldPauseSuggestions()
        {
            if (!AiEnabled) return true;
            if (_completionWindow != null) return true;
            if (!Editor.IsKeyboardFocusWithin) return true;
            if (_isScrolling) return true;

            // If selection is non-empty, many clinicians find ghost distracting; pause.
            var sel = Editor.TextArea.Selection;
            if (!sel.IsEmpty) return true;

            return false;
        }

        // ========== Ghost text ==========
        private void SetGhost(string text)
        {
            _ghostText = text ?? string.Empty;
            Editor.TextArea.TextView.InvalidateVisual();
        }

        private async System.Threading.Tasks.Task SuggestAsync()
        {
            if (ShouldPauseSuggestions())
                return;

            // swap CTS safely (cancel any prior stream)
            var newCts = new CancellationTokenSource();
            var old = Interlocked.Exchange(ref _cts, newCts);
            old?.Cancel();
            old?.Dispose();

            var ct = newCts.Token;

            var (left, right) = _adapter.GetContextWindows(512, 0);
            SetGhost(string.Empty);

            try
            {
                await foreach (var chunk in CompletionEngine.StreamAsync(new CompletionRequest(left, right, 32), ct))
                {
                    if (ct.IsCancellationRequested || ShouldPauseSuggestions())
                        break;

                    // We are on UI thread thanks to DispatcherTimer → safe to touch UI
                    SetGhost(chunk);
                }
            }
            catch (OperationCanceledException) { /* expected on rapid typing */ }
            catch { /* consider logging hook */ }
        }

        private void AcceptGhost()
        {
            if (string.IsNullOrEmpty(_ghostText)) return;
            Editor.Document.Insert(Editor.CaretOffset, _ghostText);
            SetGhost(string.Empty);
        }

        private void CancelGhost() => SetGhost(string.Empty);

        // ========== Snippet popup ==========
        private void OnTextEntered(object? s, TextCompositionEventArgs e)
        {
            // Skip snippet triggers while an IME (Korean/Japanese/Chinese) is composing
            bool imeEnabled = InputMethod.GetIsInputMethodEnabled(Editor);
            bool imeOn = InputMethod.Current?.ImeState == InputMethodState.On;
            if (imeEnabled && imeOn) return;

            if (e.Text == ";" || e.Text == ":")
                ShowSnippetCompletion();
        }

        private void OnTextEntering(object? s, TextCompositionEventArgs e)
        {
            if (_completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        private void ShowSnippetCompletion()
        {
            // If a window is open, close it and recreate (ensures fresh replace region)
            if (_completionWindow != null)
            {
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow.Close();
                _completionWindow = null;
            }

            var items = SnippetProvider.GetCompletions(Editor);
            // Guard: don’t open an empty popup
            if (!items.Any()) return;

            // Re-enumerate (provider may have lazy enumeration)
            items = SnippetProvider.GetCompletions(Editor);

            _completionWindow = MusmCompletionWindow.ShowForCurrentWord(Editor, items);
            _completionWindow.Closed += OnCompletionClosed;

            // Clear ghost while popup is open
            SetGhost(string.Empty);
        }

        private void OnCompletionClosed(object? sender, EventArgs e)
        {
            if (_completionWindow != null)
            {
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow = null;
            }
            RestartDebounce();
        }

        // ========== Pause conditions hooks ==========
        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            // Pause or resume based on selection emptiness
            if (ShouldPauseSuggestions()) { _debounce.Stop(); SetGhost(string.Empty); }
            else RestartDebounce();
        }

        private void OnScrollOffsetChanged(object? sender, EventArgs e)
        {
            _isScrolling = true;
            _scrollSilenceTimer.Stop();
            _scrollSilenceTimer.Start(); // will reset _isScrolling after a short silence
            _debounce.Stop();
            SetGhost(string.Empty);
        }

        private void OnFocusChanged(object? sender, RoutedEventArgs e)
        {
            if (ShouldPauseSuggestions()) { _debounce.Stop(); SetGhost(string.Empty); }
            else RestartDebounce();
        }

        // ===== Null objects keep MVVM clean when nothing is bound =====
        private sealed class NullCompletionEngine : ICompletionEngine
        {
            public static readonly NullCompletionEngine Instance = new();
            public async IAsyncEnumerable<string> StreamAsync(CompletionRequest req, CancellationToken ct)
            {
                await System.Threading.Tasks.Task.CompletedTask;
                yield break;
            }
        }

        private sealed class NullSnippetProvider : ISnippetProvider
        {
            public static readonly NullSnippetProvider Instance = new();
            public System.Collections.Generic.IEnumerable<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData>
                GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
                => Array.Empty<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData>();
        }
    }
}
