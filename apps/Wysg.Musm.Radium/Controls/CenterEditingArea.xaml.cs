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
      System.Diagnostics.Debug.WriteLine("[CenterEditingArea] ===== SetupEditorNavigation START =====");
  
  // Current report vertical navigation
     // IMPORTANT: Only setup Alt+Down here, Alt+Up is handled separately for orientation-aware navigation
            System.Diagnostics.Debug.WriteLine("[CenterEditingArea] Setting up EditorFindings -> EditorConclusion (Alt+Down)");
    SetupOneWayEditor(EditorFindings, EditorConclusion, Key.Down, copyText: true);
   
        // Alt+Up from EditorConclusion -> EditorFindings
            System.Diagnostics.Debug.WriteLine("[CenterEditingArea] Setting up EditorConclusion -> EditorFindings (Alt+Up)");
  SetupOneWayEditor(EditorConclusion, EditorFindings, Key.Up, copyText: true);

    // Current <-> Previous horizontal navigation
            System.Diagnostics.Debug.WriteLine("[CenterEditingArea] Setting up horizontal navigation");
          // Alt+Right from EditorFindings -> EditorPreviousFindings (no copy, just navigate)
            SetupOneWayEditor(EditorFindings, EditorPreviousFindings, Key.Right, copyText: false);
        // Alt+Left from EditorPreviousFindings -> EditorFindings (WITH copy to bring content back)
     SetupOneWayEditor(EditorPreviousFindings, EditorFindings, Key.Left, copyText: true);
      // Alt+Left from EditorPreviousHeader -> EditorFindings (with copy)
     SetupOneWayEditor(EditorPreviousHeader, EditorFindings, Key.Left, copyText: true);
// Alt+Left from EditorPreviousConclusion -> EditorFindings (with copy)
            SetupOneWayEditor(EditorPreviousConclusion, EditorFindings, Key.Left, copyText: true);
      
      // Previous report vertical navigation (no copy)
     System.Diagnostics.Debug.WriteLine("[CenterEditingArea] Setting up previous report vertical navigation");
            SetupOneWayEditor(EditorPreviousHeader, EditorPreviousFindings, Key.Down, copyText: false);
   SetupOneWayEditor(EditorPreviousFindings, EditorPreviousHeader, Key.Up, copyText: false);
         SetupOneWayEditor(EditorPreviousFindings, EditorPreviousConclusion, Key.Down, copyText: false);
       SetupOneWayEditor(EditorPreviousConclusion, EditorPreviousFindings, Key.Up, copyText: false);
        
      // SPECIAL: EditorFindings Alt+Up - orientation-aware navigation
// This MUST be setup AFTER the above to ensure it's attached last and gets priority
       System.Diagnostics.Debug.WriteLine("[CenterEditingArea] Setting up orientation-aware Alt+Up for EditorFindings");
SetupOrientationAwareUpNavigation();
      
   System.Diagnostics.Debug.WriteLine("[CenterEditingArea] ===== SetupEditorNavigation COMPLETE =====");
        }

        private void SetupOrientationAwareUpNavigation()
   {
     // This replaces the bidirectional SetupEditorPair for EditorFindings <-> EditorConclusion Alt+Up
   // The Alt+Down from EditorFindings -> EditorConclusion is already handled above
 // We need to override Alt+Up from EditorFinings to be orientation-aware
       
            System.Diagnostics.Debug.WriteLine("[CenterEditingArea] SetupOrientationAwareUpNavigation called");
      
var findingsEditor = EditorFindings.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
  if (findingsEditor == null)
       {
           System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] ERROR: Could not find Editor in EditorFindings for orientation-aware navigation");
 return;
     }
 
     System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] Found findingsEditor, attaching PreviewKeyDown handler to TextArea with AddHandler");

   // ==================================================================================
        // FIX EXPLANATION: Why AddHandler with handledEventsToo=true is CRITICAL
        // ==================================================================================
  //
        // PROBLEM:
        // The Popup handler in EditorControl.View.cs uses:
        //   Editor.TextArea.AddHandler(UIElement.PreviewKeyDownEvent, 
        //    new KeyEventHandler(OnTextAreaPreviewKeyDown), 
        //          handledEventsToo: true)
        //
        // This means it sees ALL PreviewKeyDown events, even if already marked as handled.
        //
        // We initially tried using regular event subscription (+=):
        //   findingsEditor.TextArea.PreviewKeyDown += (s, e) => { ... }
     //
        // But this ONLY receives events that have NOT been marked as handled.
        //
   // THE ROOT CAUSE:
        // When Alt+Up is pressed, multiple handlers process it:
    //   1. Popup handler (with handledEventsToo=true) - detects Alt+Arrow, passes through
        //   2. AvalonEdit's built-in navigation - marks event as handled (for text navigation)
        //   3. Our handler (with +=) - NEVER SEES IT because event is already handled!
        //
        // THE SOLUTION:
      // Use AddHandler with handledEventsToo=true to ensure our handler sees the Alt+Up
        // event even if AvalonEdit or other handlers have already marked it as handled.
        //
        // This matches the pattern used by EditorControl.Popup.cs and ensures we receive
      // ALL keyboard events at the TextArea level, allowing us to intercept Alt+Up before
   // default navigation behavior takes over.
        //
     // ADDITIONAL CONTEXT:
        // - We attach to TextArea.PreviewKeyDown (not Editor.PreviewKeyDown) to match
        //   where the Popup handler is attached, ensuring consistent event handling order
        // - The Popup handler passes through Alt+Arrow by returning early without e.Handled=true
 // - We then catch Alt+Up and execute our orientation-aware navigation logic
        // - Finally we set e.Handled=true to prevent default Up arrow navigation
        //
    // ==================================================================================
   findingsEditor.TextArea.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, e) =>
   {
     System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] TextArea.PreviewKeyDown: Key={e.Key}, SystemKey={e.SystemKey}, Modifiers={e.KeyboardDevice.Modifiers}, Handled={e.Handled}");
   
   var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
  
 System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] actualKey={actualKey}, checking for Alt+Up");
      
    // Check ONLY for Alt+Up combination (ignore standalone modifier keys)
   // We need BOTH Alt modifier AND Up key in the SAME event
      if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == Key.Up)
  {
  System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] ALT+UP DETECTED! Calling HandleOrientationAwareUpNavigation");
      HandleOrientationAwareUpNavigation();
      e.Handled = true;
     System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] Event marked as handled");
}
        }), handledEventsToo: true); // CRITICAL: true ensures we see events even if already handled by AvalonEdit
   
        System.Diagnostics.Debug.WriteLine("[CenterEditingArea] PreviewKeyDown handler attached successfully to TextArea with AddHandler(handledEventsToo=true)");
 }

        private void HandleOrientationAwareUpNavigation()
        {
     // Check orientation via injected function
            bool isLandscape = IsLandscapeMode?.Invoke() ?? false;
      
  System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] HandleOrientationAwareUpNavigation:");
   System.Diagnostics.Debug.WriteLine($"  - isLandscape: {isLandscape}");
      
       if (isLandscape)
  {
       // Landscape mode: Navigate to Patient History textbox in gridSideTop
        var txtPatientHistory = GetPatientHistoryTextBox?.Invoke();
      System.Diagnostics.Debug.WriteLine($"  - Landscape branch: txtPatientHistory={txtPatientHistory != null}");
  if (txtPatientHistory != null)
        {
      System.Diagnostics.Debug.WriteLine($"  - Focusing Patient History (landscape)");
    txtPatientHistory.Focus();
 txtPatientHistory.CaretIndex = txtPatientHistory.Text?.Length ?? 0;
       }
       else
     {
         System.Diagnostics.Debug.WriteLine($"  - Patient History textbox not found, falling back to EditorConclusion");
       // Fallback to EditorConclusion if Patient History not available
   HandleEditorNavigation(EditorFindings, EditorConclusion, copyText: true);
  }
       }
   else
   {
   // Portrait mode: Navigate to Patient History textbox (should be available via GetPatientHistoryTextBox)
       var txtPatientHistory = GetPatientHistoryTextBox?.Invoke();
 System.Diagnostics.Debug.WriteLine($"  - Portrait branch: txtPatientHistory={txtPatientHistory != null}");
      if (txtPatientHistory != null)
 {
       System.Diagnostics.Debug.WriteLine($"  - Focusing Patient History (portrait)");
  txtPatientHistory.Focus();
         txtPatientHistory.CaretIndex = txtPatientHistory.Text?.Length ?? 0;
      }
  else
          {
         System.Diagnostics.Debug.WriteLine($"  - Patient History textbox not found");
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
  if (sourceEditor == null)
        {
           System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] Could not find Editor in source control");
     return;
            }

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
