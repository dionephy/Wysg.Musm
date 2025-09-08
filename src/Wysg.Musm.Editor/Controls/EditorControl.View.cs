using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Ghosting;
using Wysg.Musm.Editor.Snippets;

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

        public IGhostSuggestionClient? GhostClient { get => (IGhostSuggestionClient?)GetValue(GhostClientProperty); set => SetValue(GhostClientProperty, value); }
        public string PatientSex { get => (string)GetValue(PatientSexProperty); set => SetValue(PatientSexProperty, value); }
        public int PatientAge { get => (int)GetValue(PatientAgeProperty); set => SetValue(PatientAgeProperty, value); }
        public string StudyHeader { get => (string)GetValue(StudyHeaderProperty); set => SetValue(StudyHeaderProperty, value); }
        public string StudyInfo { get => (string)GetValue(StudyInfoProperty); set => SetValue(StudyInfoProperty, value); }

        // ===== Commands (unchanged) =====
        public ICommand ForceSuggestCommand { get; } = new RoutedUICommand("ForceSuggest", "ForceSuggest", typeof(EditorControl));
        public ICommand AcceptGhostCommand { get; } = new RoutedUICommand("AcceptGhost", "AcceptGhost", typeof(EditorControl));
        public ICommand CancelGhostCommand { get; } = new RoutedUICommand("CancelGhost", "CancelGhost", typeof(EditorControl));

        // ===== Shared state & timers used by other partials =====
        private readonly DispatcherTimer _debounce;
        private bool _isScrolling;

        // Popup handle (owned by Popup partial)
        private MusmCompletionWindow? _completionWindow;

        public EditorControl()
        {
            InitializeComponent();

            // Core editor hooks (common to all behaviors)
            Editor.TextArea.TextEntered += OnTextEntered;       // declared in Popup partial
            Editor.TextArea.TextEntering += OnTextEntering;     // declared in Popup partial
            Editor.TextArea.PreviewKeyDown += OnTextAreaPreviewKeyDown; // declared in Popup partial
            try { Editor.TextArea.SelectionChanged += OnSelectionChanged; } catch { }
            Editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;
            Editor.GotKeyboardFocus += OnFocusChanged;
            Editor.LostKeyboardFocus += OnFocusChanged;

            // Debounce for inline-ghost (if enabled)
            _debounce = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMs)
            };
            _debounce.Tick += OnDebounceTick;

            // Feature init (implemented in their own partials)
            InitPopup();        // EditorControl.Popup.cs
            InitServerGhosts(); // EditorControl.ServerGhosts.cs
            InitInlineGhost();  // EditorControl.InlineGhost.cs (no-op if you choose)

            // Disable Tab insertion so Tab can accept ghost or popup
            Loaded += (_, __) => DisableTabInsertion();

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
            if (!(bool)e.NewValue) self._debounce.Stop();
            else self.RestartDebounce();
        }

        // ===== Lifecycle cleanup =====
        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _debounce.Stop();
            _debounce.Tick -= OnDebounceTick;

            CleanupPopup();        // Popup partial
            CleanupServerGhosts(); // ServerGhosts partial
            CleanupInlineGhost();  // InlineGhost partial

            Editor.TextArea.TextEntered -= OnTextEntered;
            Editor.TextArea.TextEntering -= OnTextEntering;
            Editor.TextArea.PreviewKeyDown -= OnTextAreaPreviewKeyDown;
            try { Editor.TextArea.SelectionChanged -= OnSelectionChanged; } catch { }
            Editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChanged;
            Editor.GotKeyboardFocus -= OnFocusChanged;
            Editor.LostKeyboardFocus -= OnFocusChanged;
        }

        // ===== Shared helpers used by partials =====
        private void RestartDebounce()
        {
            if (ShouldPauseSuggestions()) return;
            _debounce.Stop();
            _debounce.Start();
        }

        private void OnSelectionChanged(object? s, EventArgs e)
        {
            if (ShouldPauseSuggestions()) _debounce.Stop();
            else RestartDebounce();
        }

        private void OnScrollOffsetChanged(object? s, EventArgs e)
        {
            _isScrolling = true;
            // a tiny “scroll silence” can live in InlineGhost partial if needed
            _debounce.Stop();
        }

        private void OnFocusChanged(object? s, RoutedEventArgs e)
        {
            if (ShouldPauseSuggestions()) _debounce.Stop();
            else RestartDebounce();
        }

        // overriden in InlineGhost partial; keep a conservative default here
        private bool ShouldPauseSuggestions() =>
            !AiEnabled || !Editor.IsKeyboardFocusWithin || _isScrolling;

        // 1) Turn this into a partial method declaration (no body here)
        partial void DisableTabInsertion();

        // 2) Add no-op partial hooks if you removed the InlineGhost file
        partial void InitInlineGhost();
        partial void CleanupInlineGhost();

        // 3) Null objects (were in the monolith file; bring them back here)
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
            public System.Collections.Generic.IEnumerable<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData>
                GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
                => Array.Empty<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData>();
        }
    }
}
