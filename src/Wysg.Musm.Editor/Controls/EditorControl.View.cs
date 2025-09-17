using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Editor.Ui;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl : UserControl
    {
        // ===== Dependency Properties =====
        public static readonly DependencyProperty DocumentTextProperty =
            DependencyProperty.Register(nameof(DocumentText), typeof(string), typeof(EditorControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty AiEnabledProperty =
            DependencyProperty.Register(nameof(AiEnabled), typeof(bool), typeof(EditorControl),
                new PropertyMetadata(true, OnAiEnabledChanged));

        public static readonly DependencyProperty DebounceMsProperty =
            DependencyProperty.Register(nameof(DebounceMs), typeof(int), typeof(EditorControl),
                new PropertyMetadata(200, OnDebounceMsChanged));

        public static readonly DependencyProperty AutoSuggestOnTypingProperty =
            DependencyProperty.Register(nameof(AutoSuggestOnTyping), typeof(bool), typeof(EditorControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty MinCharsForSuggestProperty =
            DependencyProperty.Register(nameof(MinCharsForSuggest), typeof(int), typeof(EditorControl),
                new PropertyMetadata(2));

        public static readonly DependencyProperty GhostIdleMsProperty =
    DependencyProperty.Register(nameof(GhostIdleMs), typeof(int), typeof(EditorControl),
        new PropertyMetadata(2000, OnGhostIdleMsChanged));

        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(HorizontalScrollBarVisibility),
                typeof(System.Windows.Controls.ScrollBarVisibility),
                typeof(EditorControl),
                new PropertyMetadata(System.Windows.Controls.ScrollBarVisibility.Disabled));

        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(VerticalScrollBarVisibility),
                typeof(System.Windows.Controls.ScrollBarVisibility),
                typeof(EditorControl),
                new PropertyMetadata(System.Windows.Controls.ScrollBarVisibility.Auto));

        public int GhostIdleMs
        {
            get => (int)GetValue(GhostIdleMsProperty);
            set => SetValue(GhostIdleMsProperty, value);
        }

        public System.Windows.Controls.ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => (System.Windows.Controls.ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
            set => SetValue(HorizontalScrollBarVisibilityProperty, value);
        }
        public System.Windows.Controls.ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (System.Windows.Controls.ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        private static void OnGhostIdleMsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (EditorControl)d;
            int ms = Math.Max(250, (int)e.NewValue);      // guard against tiny values
            self._idleTimer.Interval = TimeSpan.FromMilliseconds(ms);
        }


        // Context for server ghosts (used by Playground VM)
        public static readonly DependencyProperty PatientSexProperty =
            DependencyProperty.Register(nameof(PatientSex), typeof(string), typeof(EditorControl), new PropertyMetadata("M"));
        public static readonly DependencyProperty PatientAgeProperty =
            DependencyProperty.Register(nameof(PatientAge), typeof(int), typeof(EditorControl), new PropertyMetadata(60));
        public static readonly DependencyProperty StudyHeaderProperty =
            DependencyProperty.Register(nameof(StudyHeader), typeof(string), typeof(EditorControl), new PropertyMetadata(""));

        public static readonly DependencyProperty StudyInfoProperty =
            DependencyProperty.Register(nameof(StudyInfo), typeof(string), typeof(EditorControl), new PropertyMetadata(""));

        // Completion/snippets pluggables
        public static readonly DependencyProperty SnippetProviderProperty =
            DependencyProperty.Register(nameof(SnippetProvider), typeof(ISnippetProvider), typeof(EditorControl),
                new PropertyMetadata(null));

        // CLR wrappers
        public string DocumentText { get => (string)GetValue(DocumentTextProperty); set => SetValue(DocumentTextProperty, value); }
        public bool AiEnabled { get => (bool)GetValue(AiEnabledProperty); set => SetValue(AiEnabledProperty, value); }
        public int DebounceMs { get => (int)GetValue(DebounceMsProperty); set => SetValue(DebounceMsProperty, value); }
        public bool AutoSuggestOnTyping { get => (bool)GetValue(AutoSuggestOnTypingProperty); set => SetValue(AutoSuggestOnTypingProperty, value); }
        public int MinCharsForSuggest { get => (int)GetValue(MinCharsForSuggestProperty); set => SetValue(MinCharsForSuggestProperty, value); }

        public string PatientSex { get => (string)GetValue(PatientSexProperty); set => SetValue(PatientSexProperty, value); }
        public int PatientAge { get => (int)GetValue(PatientAgeProperty); set => SetValue(PatientAgeProperty, value); }
        public string StudyHeader { get => (string)GetValue(StudyHeaderProperty); set => SetValue(StudyHeaderProperty, value); }
        public string StudyInfo { get => (string)GetValue(StudyInfoProperty); set => SetValue(StudyInfoProperty, value); }
        public ISnippetProvider? SnippetProvider { get => (ISnippetProvider?)GetValue(SnippetProviderProperty); set => SetValue(SnippetProviderProperty, value); }

        // ===== Timers / state =====
        private readonly DispatcherTimer _debounce;          // short debounce (typing/caret)
        private readonly DispatcherTimer _idleTimer;         // 2s idle → request server ghosts
        private bool _isScrolling;
        private bool _idlePausedForGhosts;



        // ===== Popup & Renderer handles (implemented elsewhere) =====
        private MusmCompletionWindow? _completionWindow;     // used by Popup partial
        private MultiLineGhostRenderer? _ghostRenderer;      // ghost renderer

        // ===== Idle event for VM =====
        public event EventHandler? IdleElapsed;

        public EditorControl()
        {
            InitializeComponent();

            ServerGhosts = new GhostStore(InvalidateGhosts);

            // ── 1) Robust scheduling: restart both short debounce (if you still use it)
            //    and the 2s idle for server ghosts on any text change
            Editor.TextChanged += (_, __) =>
            {
                RestartDebounce();   // your existing short debounce (e.g., inline/UX stuff)
                RestartIdle();       // 2s idle for server ghosts
            };

            // Optional: caret moves can also restart idle if you want (kept off by default)
            Editor.TextArea.Caret.PositionChanged += (_, __) => { /* no-op */ };

            // ── 2) Visual highlighter for the current “word of interest”
            _wordHi = new Ui.CurrentWordHighlighter(Editor, () =>
            {
                if (_completionWindow == null || _lastWordStart < 0) return null;
                var caret = Editor.CaretOffset;
                var len = Math.Max(0, caret - _lastWordStart);
                return new Wysg.Musm.Editor.Internal.InlineSegment(_lastWordStart, len);
            });

            // ── 3) Core hooks (popup + focus/scroll awareness)
            Editor.TextArea.TextEntered += OnTextEntered;                 // Popup partial
            Editor.TextArea.TextEntering += OnTextEntering;               // Popup partial
            // Use AddHandler to receive PreviewKeyDown even if already handled by other listeners
            Editor.TextArea.AddHandler(UIElement.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(OnTextAreaPreviewKeyDown), true);
            try { Editor.TextArea.SelectionChanged += OnSelectionChanged; } catch { }
            Editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;
            Editor.GotKeyboardFocus += OnFocusChanged;
            Editor.LostKeyboardFocus += OnFocusChanged;

            // ── 4) Short debounce (kept if you have other 150–300ms UX)
            _debounce = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMs)
            };
            _debounce.Tick += OnDebounceTick;

            // ── 5) Dedicated 2s idle timer ONLY for server ghosts
            _idleTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _idleTimer.Tick += OnIdleTimerTick;

            // ── 6) Feature init (other partials)
            InitPopup();        // EditorControl.Popup.cs
            InitServerGhosts(); // EditorControl.ServerGhosts.cs
            InitInlineGhost();  // optional partial

            // ── 7) Tab is used for accept; prevent raw '\t' insertion
            Loaded += (_, __) => DisableTabInsertion();

            Unloaded += OnUnloaded;

            // ── 8) Bubble a few editor signals (your existing)
            Editor.TextChanged += (_, __) => RaiseEditorTextChanged();
            Editor.TextArea.Caret.PositionChanged += (_, __) => RaiseEditorCaretMoved();

            // ── 9) Attach multiline-ghost renderer explicitly
            var view = Editor.TextArea.TextView;
            var area = Editor.TextArea;

            _ghostRenderer = new MultiLineGhostRenderer(
                view,
                area,
                () => ServerGhosts.Items,
                getSelectedIndex: () => ServerGhosts.SelectedIndex,
                typeface: new Typeface(Editor.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                getFontSize: () => Editor.FontSize,
                showAnchors: false);
            _ghostRenderer.Attach();
        }


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
                self._debounce.Stop();
                self._idleTimer.Stop();
            }
            else
            {
                self.RestartDebounce();
            }
        }

        private void OnDebounceTick(object? s, EventArgs e)
        {
            _debounce.Stop();

            // Debounce is for lightweight UX (e.g., updating completion-ghost preview),
            // NEVER for server calls and NEVER for closing the popup.
            if (!AiEnabled) return;

            // If you had code like CloseCompletionWindow(); or RequestServerGhostsAsync(); here,
            // delete it. Leave this either empty or only UI-local work.
        }

        // Minimal “bubblers” — also good places to reset idle countdown
        private void RaiseEditorTextChanged()
        {
            RestartDebounce();
            RestartIdle();
        }
        private void RaiseEditorCaretMoved()
        {
            RestartDebounce();
            RestartIdle();
        }

        private void RestartDebounce()
        {
            if (ShouldPauseSuggestions()) return;
            _debounce.Stop();
            _debounce.Start();
        }

        private void RestartIdle()
        {
            if (_idlePausedForGhosts) return;
            _idleTimer.Stop();
            _idleTimer.Start();
        }

        private void OnSelectionChanged(object? s, EventArgs e)
        {
            if (ShouldPauseSuggestions()) { _debounce.Stop(); _idleTimer.Stop(); }
            else { RestartDebounce(); RestartIdle(); }
        }

        private void OnScrollOffsetChanged(object? s, EventArgs e)
        {
            _isScrolling = true;
            _debounce.Stop();
            _idleTimer.Stop();
            // give a short grace period: when scrolling stops, caret move or text change will restart timers
        }

        private void OnFocusChanged(object? s, RoutedEventArgs e)
        {
            if (ShouldPauseSuggestions()) { _debounce.Stop(); _idleTimer.Stop(); }
            else { RestartDebounce(); RestartIdle(); }
        }

        // Conservative default; InlineGhost partial can override via another partial method if needed
        private bool ShouldPauseSuggestions() =>
            !AiEnabled || !Editor.IsKeyboardFocusWithin || _isScrolling;

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _debounce.Stop();
            _debounce.Tick -= OnDebounceTick;
            _idleTimer.Stop();

            CleanupPopup();
            CleanupServerGhosts();
            CleanupInlineGhost();

            Editor.TextArea.TextEntered -= OnTextEntered;
            Editor.TextArea.TextEntering -= OnTextEntering;
            // remove handler added with AddHandler
            Editor.TextArea.RemoveHandler(UIElement.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(OnTextAreaPreviewKeyDown));
            try { Editor.TextArea.SelectionChanged -= OnSelectionChanged; } catch { }
            Editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChanged;
            Editor.GotKeyboardFocus -= OnFocusChanged;
            Editor.LostKeyboardFocus -= OnFocusChanged;

            _ghostRenderer?.Detach();
            _ghostRenderer = null;
        }

        // Fires only after 2s of no input (popup will be closed here)
        private void OnIdleTimerTick(object? s, EventArgs e)
        {
            _idleTimer.Stop();
            if (!AiEnabled) return;
            if (_idlePausedForGhosts) return;

            // spec: close popup on idle, then ask server
            CloseCompletionWindow();
            _ = RequestServerGhostsAsync();  // this just raises IdleElapsed in your current setup
        }


        // Raise the event so the Playground/VM performs the server call.
        // You can inline the API call here later if you prefer.
        private async Task RequestServerGhostsAsync()
        {
            IdleElapsed?.Invoke(this, EventArgs.Empty);
            await Task.CompletedTask;
        }


        // ===== Dummy ICommand properties for XAML bindings =====
        public ICommand ForceSuggestCommand { get; } = new DummyCommand();
        public ICommand AcceptGhostCommand { get; } = new DummyCommand();
        public ICommand CancelGhostCommand { get; } = new DummyCommand();

        private class DummyCommand : ICommand
        {
            public event System.EventHandler? CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) { /* no-op */ }
        }
    }
}
