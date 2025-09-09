using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Ghosting;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Editor.Ui;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl : UserControl
    {
        // ===== Dependency Properties (view + popup) =====
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

        public static readonly DependencyProperty AutoSuggestOnTypingProperty =
            DependencyProperty.Register(nameof(AutoSuggestOnTyping), typeof(bool), typeof(EditorControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty MinCharsForSuggestProperty =
            DependencyProperty.Register(nameof(MinCharsForSuggest), typeof(int), typeof(EditorControl),
                new PropertyMetadata(2));

        // ===== Idle timer (server ghost trigger) =====
        public static readonly DependencyProperty ServerIdleMsProperty =
            DependencyProperty.Register(nameof(ServerIdleMs), typeof(int), typeof(EditorControl),
                new PropertyMetadata(2000, OnServerIdleMsChanged));

        // ===== Dependency Properties (server ghost context) =====
        public static readonly DependencyProperty GhostClientProperty =
           DependencyProperty.Register(nameof(GhostClient), typeof(IGhostSuggestionClient), typeof(EditorControl),
               new PropertyMetadata(null));

        public static readonly DependencyProperty PatientSexProperty =
            DependencyProperty.Register(nameof(PatientSex), typeof(string), typeof(EditorControl), new PropertyMetadata("M"));

        public static readonly DependencyProperty PatientAgeProperty =
            DependencyProperty.Register(nameof(PatientAge), typeof(int), typeof(EditorControl), new PropertyMetadata(60));

        public static readonly DependencyProperty StudyHeaderProperty =
            DependencyProperty.Register(nameof(StudyHeader), typeof(string), typeof(EditorControl), new PropertyMetadata(""));

        public static readonly DependencyProperty StudyInfoProperty =
            DependencyProperty.Register(nameof(StudyInfo), typeof(string), typeof(EditorControl), new PropertyMetadata(""));

        // DP CLR wrappers
        public string DocumentText { get => (string)GetValue(DocumentTextProperty); set => SetValue(DocumentTextProperty, value); }
        public bool AiEnabled { get => (bool)GetValue(AiEnabledProperty); set => SetValue(AiEnabledProperty, value); }
        public ICompletionEngine CompletionEngine { get => (ICompletionEngine)GetValue(CompletionEngineProperty); set => SetValue(CompletionEngineProperty, value); }
        public ISnippetProvider SnippetProvider { get => (ISnippetProvider)GetValue(SnippetProviderProperty); set => SetValue(SnippetProviderProperty, value); }
        public int DebounceMs { get => (int)GetValue(DebounceMsProperty); set => SetValue(DebounceMsProperty, value); }
        public bool AutoSuggestOnTyping { get => (bool)GetValue(AutoSuggestOnTypingProperty); set => SetValue(AutoSuggestOnTypingProperty, value); }
        public int MinCharsForSuggest { get => (int)GetValue(MinCharsForSuggestProperty); set => SetValue(MinCharsForSuggestProperty, value); }

        public int ServerIdleMs { get => (int)GetValue(ServerIdleMsProperty); set => SetValue(ServerIdleMsProperty, value); }

        public IGhostSuggestionClient? GhostClient { get => (IGhostSuggestionClient?)GetValue(GhostClientProperty); set => SetValue(GhostClientProperty, value); }
        public string PatientSex { get => (string)GetValue(PatientSexProperty); set => SetValue(PatientSexProperty, value); }
        public int PatientAge { get => (int)GetValue(PatientAgeProperty); set => SetValue(PatientAgeProperty, value); }
        public string StudyHeader { get => (string)GetValue(StudyHeaderProperty); set => SetValue(StudyHeaderProperty, value); }
        public string StudyInfo { get => (string)GetValue(StudyInfoProperty); set => SetValue(StudyInfoProperty, value); }

        // ===== Events the host/VM can subscribe to =====
        public event EventHandler? IdleElapsed;           // fired after 2s of no activity (when not paused)
        public event EventHandler? EditorTextChanged;
        public event EventHandler? EditorCaretMoved;

        // ===== Public surface used by Playground etc. =====
        public TextEditor InnerEditor => Editor;
        public bool IsCompletionWindowOpen => _completionWindow?.IsVisible == true;
        public bool IsInPlaceholderMode => PlaceholderModeManager.IsActive;

        // ===== Shared state & timers =====
        private readonly DispatcherTimer _debounce;     // short debounce for local UI (not server)
        private readonly DispatcherTimer _idle;         // 2s idle → let VM call server
        private readonly DispatcherTimer _scrollQuiet;  // resets _isScrolling after a short delay
        private bool _isScrolling;

        // Popup handle (owned by Popup partial)
        private CompletionWindow? _completionWindow;

        // Multiline ghost renderer handle
        private MultiLineGhostRenderer? _ghostRenderer;

        public EditorControl()
        {
            InitializeComponent();

            // ---- timers ----
            _debounce = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMs)
            };
            _debounce.Tick += OnDebounceTick;

            _idle = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(ServerIdleMs)
            };
            _idle.Tick += OnIdleTick;

            _scrollQuiet = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _scrollQuiet.Tick += (_, __) => { _scrollQuiet.Stop(); _isScrolling = false; };

            // ---- editor hooks ----
            Editor.TextArea.TextEntered += OnTextEntered;             // Popup partial
            Editor.TextArea.TextEntering += OnTextEntering;           // Popup partial
            Editor.TextArea.PreviewKeyDown += OnTextAreaPreviewKeyDown; // Popup + Ghost nav
            try { Editor.TextArea.SelectionChanged += OnSelectionChanged; } catch { /* some versions */ }
            Editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;
            Editor.GotKeyboardFocus += OnFocusChanged;
            Editor.LostKeyboardFocus += OnFocusChanged;

            // Forward out to host if needed
            Editor.TextChanged += (_, __) => { RaiseEditorTextChanged(); RestartDebounce(); RestartIdle(); };
            Editor.TextArea.Caret.PositionChanged += (_, __) => { RaiseEditorCaretMoved(); RestartDebounce(); RestartIdle(); };
            Editor.TextArea.PreviewKeyDown += (_, __) => RestartIdle();
            Editor.TextArea.TextEntered += (_, __) => RestartIdle();
            Editor.TextArea.TextEntering += (_, __) => RestartIdle();

            // ---- features (other partials) ----
            InitPopup();        // EditorControl.Popup.cs
            InitServerGhosts(); // EditorControl.ServerGhosts.cs
            InitInlineGhost();  // if you keep inline ghost; otherwise this is empty

            // Disable tab insertion so Tab can be used for accept (ghost/snippet)
            Loaded += (_, __) => DisableTabInsertion();

            // Cleanup
            Unloaded += OnUnloaded;

            // Attach multiline ghost renderer
            var view = Editor.TextArea.TextView;
            var area = Editor.TextArea;

            if (_ghostRenderer is null)
            {
                var tf = new Typeface(Editor.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                _ghostRenderer = new MultiLineGhostRenderer(
                    view,
                    area,
                    () => ServerGhosts.Items,
                    GetSelectedGhostIndex,   // selection index provider
                    tf,
                    () => Editor.FontSize,
                    showAnchors: false       // set true if you want orange dots for debug
                );
                _ghostRenderer.Attach(); // explicit attach + internal logging
            }
        }

        // ===== Property change callbacks =====
        private static void OnDebounceMsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            self._debounce.Interval = TimeSpan.FromMilliseconds(Math.Max(0, (int)e.NewValue));
        }

        private static void OnServerIdleMsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            self._idle.Interval = TimeSpan.FromMilliseconds(Math.Max(0, (int)e.NewValue));
        }

        private static void OnAiEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            if (!(bool)e.NewValue)
            {
                self._debounce.Stop();
                self._idle.Stop();
            }
            else
            {
                self.RestartDebounce();
                self.RestartIdle();
            }
        }

        // ===== Lifecycle cleanup =====
        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _debounce.Stop(); _debounce.Tick -= OnDebounceTick;
            _idle.Stop(); _idle.Tick -= OnIdleTick;
            _scrollQuiet.Stop();

            CleanupPopup();
            CleanupServerGhosts();
            CleanupInlineGhost();

            Editor.TextArea.TextEntered -= OnTextEntered;
            Editor.TextArea.TextEntering -= OnTextEntering;
            Editor.TextArea.PreviewKeyDown -= OnTextAreaPreviewKeyDown;
            try { Editor.TextArea.SelectionChanged -= OnSelectionChanged; } catch { }
            Editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChanged;
            Editor.GotKeyboardFocus -= OnFocusChanged;
            Editor.LostKeyboardFocus -= OnFocusChanged;

            _ghostRenderer?.Detach();
            _ghostRenderer = null;
        }

        // ===== Shared helpers used by partials =====
        private void RestartDebounce()
        {
            _debounce.Stop();
            if (!ShouldPauseSuggestions()) _debounce.Start();
        }

        private void RestartIdle()
        {
            _idle.Stop();
            if (!ShouldPauseSuggestions()) _idle.Start();
        }

        private void OnDebounceTick(object? sender, EventArgs e)
        {
            // keep for local UI debounced work (if any). For now, just stop.
            _debounce.Stop();
        }

        private void OnIdleTick(object? sender, EventArgs e)
        {
            _idle.Stop();
            if (!ShouldPauseSuggestions())
                IdleElapsed?.Invoke(this, EventArgs.Empty); // VM listens and calls server
        }

        private void OnSelectionChanged(object? s, EventArgs e)
        {
            if (ShouldPauseSuggestions())
            {
                _debounce.Stop();
                _idle.Stop();
            }
            else
            {
                RestartDebounce();
                RestartIdle();
            }
        }

        private void OnScrollOffsetChanged(object? s, EventArgs e)
        {
            _isScrolling = true;
            _idle.Stop();
            _scrollQuiet.Stop();
            _scrollQuiet.Start();
        }

        private void OnFocusChanged(object? s, RoutedEventArgs e)
        {
            if (ShouldPauseSuggestions())
            {
                _debounce.Stop();
                _idle.Stop();
            }
            else
            {
                RestartDebounce();
                RestartIdle();
            }
        }

        private bool ShouldPauseSuggestions() =>
            !AiEnabled
            || !Editor.IsKeyboardFocusWithin
            || _isScrolling
            || (ServerGhosts.Items.Count > 0)           // showing ghosts → hold idle
            || (_completionWindow?.IsVisible == true)   // completion popup open
            || PlaceholderModeManager.IsActive;         // snippet placeholder mode

        // partial hooks implemented in other files
        partial void DisableTabInsertion();
        partial void InitInlineGhost();
        partial void CleanupInlineGhost();

        // raise outward
        private void RaiseEditorTextChanged() => EditorTextChanged?.Invoke(this, EventArgs.Empty);
        private void RaiseEditorCaretMoved() => EditorCaretMoved?.Invoke(this, EventArgs.Empty);

        // Nulls
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
