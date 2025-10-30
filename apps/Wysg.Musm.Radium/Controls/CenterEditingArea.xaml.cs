using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Radium.Controls
{
    /// <summary>
    /// UserControl combining current and previous report editing panels.
    /// This is the central editing area in MainWindow (replaces the former gridCenter).
    /// </summary>
    public partial class CenterEditingArea : UserControl
    {
 public CenterEditingArea()
        {
  InitializeComponent();
        
 // Wire up ExtractPhrases event from CurrentReportPanel to bubble to MainWindow
   CurrentReportPanel.ExtractPhrasesClick += OnExtractPhrasesClick;
      
       // Setup Alt+Arrow navigation between editors
            Loaded += (_, __) => SetupEditorNavigation();
        }

        /// <summary>
        /// Event raised when Extract Phrases button is clicked.
        /// Bubbles from CurrentReportPanel to MainWindow.
        /// </summary>
        public event RoutedEventHandler? ExtractPhrasesClick;

    private void OnExtractPhrasesClick(object? sender, RoutedEventArgs e)
 {
     // Bubble event to parent (MainWindow)
            ExtractPhrasesClick?.Invoke(this, e);
        }

        // Public accessors for nested editor controls (for MainWindow.xaml.cs access)
        public EditorControl EditorHeader => CurrentReportPanel.HeaderEditor;
        public EditorControl EditorFindings => CurrentReportPanel.FindingsEditor;
   public EditorControl EditorConclusion => CurrentReportPanel.ConclusionEditor;
        public EditorControl EditorPreviousHeader => PreviousReportPanel.PreviousHeaderEditor;
        public EditorControl EditorPreviousFindings => PreviousReportPanel.PreviousFindingsEditor;
      public EditorControl EditorPreviousConclusion => PreviousReportPanel.PreviousConclusionEditor;

     // Public property to allow MainWindow to inject orientation detection
    public System.Func<bool>? IsLandscapeMode { get; set; }
        
    // Public property to allow MainWindow to inject Patient History textbox getter
        public System.Func<TextBox?>? GetPatientHistoryTextBox { get; set; }

        private void SetupEditorNavigation()
        {
  // Current report vertical navigation
            // IMPORTANT: Only setup Alt+Down here, Alt+Up is handled separately for orientation-aware navigation
      SetupOneWayEditor(EditorFindings, EditorConclusion, Key.Down, copyText: true);
  
            // Alt+Up from EditorConclusion -> EditorFindings
            SetupOneWayEditor(EditorConclusion, EditorFindings, Key.Up, copyText: true);

       // Current <-> Previous horizontal navigation
            // Alt+Right from EditorFindings -> EditorPreviousFindings (no copy, just navigate)
  SetupOneWayEditor(EditorFindings, EditorPreviousFindings, Key.Right, copyText: false);
   // Alt+Left from EditorPreviousFindings -> EditorFindings (WITH copy to bring content back)
            SetupOneWayEditor(EditorPreviousFindings, EditorFindings, Key.Left, copyText: true);
            // Alt+Left from EditorPreviousHeader -> EditorFindings (with copy)
     SetupOneWayEditor(EditorPreviousHeader, EditorFindings, Key.Left, copyText: true);
 // Alt+Left from EditorPreviousConclusion -> EditorFindings (with copy)
     SetupOneWayEditor(EditorPreviousConclusion, EditorFindings, Key.Left, copyText: true);
       
            // Previous report vertical navigation (no copy)
  SetupOneWayEditor(EditorPreviousHeader, EditorPreviousFindings, Key.Down, copyText: false);
            SetupOneWayEditor(EditorPreviousFindings, EditorPreviousHeader, Key.Up, copyText: false);
   SetupOneWayEditor(EditorPreviousFindings, EditorPreviousConclusion, Key.Down, copyText: false);
   SetupOneWayEditor(EditorPreviousConclusion, EditorPreviousFindings, Key.Up, copyText: false);
  
// SPECIAL: EditorFindings Alt+Up - orientation-aware navigation
        // This MUST be setup AFTER the above to ensure it's attached last and gets priority
     SetupOrientationAwareUpNavigation();
        }

        private void SetupOrientationAwareUpNavigation()
        {
            // This replaces the bidirectional SetupEditorPair for EditorFindings <-> EditorConclusion Alt+Up
    // The Alt+Down from EditorFindings -> EditorConclusion is already handled above
            // We need to override Alt+Up from EditorFinings to be orientation-aware
      
     var findingsEditor = EditorFindings.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
            if (findingsEditor == null) return;

            // Use AddHandler with handledEventsToo=true to ensure our handler sees the Alt+Up
            // event even if AvalonEdit or other handlers have already marked it as handled
   findingsEditor.TextArea.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, e) =>
      {
             var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
          
    // Check ONLY for Alt+Up combination (ignore standalone modifier keys)
    if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == Key.Up)
      {
        HandleOrientationAwareUpNavigation();
         e.Handled = true;
          }
 }), handledEventsToo: true);
        }

      private void HandleOrientationAwareUpNavigation()
  {
          // Check orientation via injected function
       bool isLandscape = IsLandscapeMode?.Invoke() ?? false;
            
        if (isLandscape)
      {
    // Landscape mode: Navigate to Patient History textbox in gridSideTop
   var txtPatientHistory = GetPatientHistoryTextBox?.Invoke();
        if (txtPatientHistory != null)
                {
     txtPatientHistory.Focus();
        txtPatientHistory.CaretIndex = txtPatientHistory.Text?.Length ?? 0;
        }
                else
              {
        // Fallback to EditorConclusion if Patient History not available
          HandleEditorNavigation(EditorFindings, EditorConclusion, copyText: true);
    }
       }
   else
          {
          // Portrait mode: Navigate to Patient History textbox
  var txtPatientHistory = GetPatientHistoryTextBox?.Invoke();
  if (txtPatientHistory != null)
                {
           txtPatientHistory.Focus();
              txtPatientHistory.CaretIndex = txtPatientHistory.Text?.Length ?? 0;
            }
  }
    }

   private void SetupEditorPair(EditorControl source, EditorControl target, Key sourceKey, Key targetKey, bool copyText)
        {
   SetupOneWayEditor(source, target, sourceKey, copyText);
SetupOneWayEditor(target, source, targetKey, copyText);
        }

  private void SetupOneWayEditor(EditorControl source, EditorControl target, Key key, bool copyText)
        {
         // Find the underlying MusmEditor (AvalonEdit TextEditor)
            var sourceEditor = source.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
       if (sourceEditor == null) return;

            sourceEditor.PreviewKeyDown += (s, e) =>
  {
                var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
     
  if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == key)
       {
       HandleEditorNavigation(source, target, copyText);
                 e.Handled = true;
    }
            };
        }

     private void HandleEditorNavigation(EditorControl source, EditorControl target, bool copyText)
        {
   var sourceEditor = source.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
      var targetEditor = target.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
   
      if (sourceEditor == null || targetEditor == null) return;

   if (copyText && !string.IsNullOrEmpty(sourceEditor.SelectedText))
        {
          // Has selection and copying enabled: copy to end of target
      var selectedText = sourceEditor.SelectedText;
      var targetText = targetEditor.Text ?? string.Empty;
    
       if (!string.IsNullOrEmpty(targetText))
        {
           targetEditor.Text = targetText + "\n" + selectedText;
  }
         else
      {
    targetEditor.Text = selectedText;
      }
         
                targetEditor.Focus();
      targetEditor.CaretOffset = targetEditor.Text.Length;
            }
        else
            {
     // No selection or copying disabled: just move focus
             targetEditor.Focus();
 targetEditor.CaretOffset = targetEditor.Text?.Length ?? 0;
            }
        }
    }
}
