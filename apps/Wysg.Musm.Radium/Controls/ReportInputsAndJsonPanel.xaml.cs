using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Radium.Controls
{
    public partial class ReportInputsAndJsonPanel : UserControl
    {
        public ReportInputsAndJsonPanel()
        {
            InitializeComponent();
            Loaded += (_, __) => 
            {
                ApplyReverse(Reverse);
                SetupAltArrowNavigation();
            };
        }

        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(nameof(Reverse), typeof(bool), typeof(ReportInputsAndJsonPanel), new PropertyMetadata(false, OnReverseChanged));

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }

        // Dependency property for the target EditorControl (EditorFindings from CurrentReportEditorPanel)
        public static readonly DependencyProperty TargetEditorProperty =
  DependencyProperty.Register(nameof(TargetEditor), typeof(EditorControl), typeof(ReportInputsAndJsonPanel), 
         new PropertyMetadata(null, OnTargetEditorChanged));

        public EditorControl? TargetEditor
        {
    get => (EditorControl?)GetValue(TargetEditorProperty);
            set => SetValue(TargetEditorProperty, value);
        }

        private static void OnTargetEditorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        if (d is ReportInputsAndJsonPanel self)
  {
    // Re-setup navigation when target editor changes
       self.SetupAltArrowNavigation();
       }
        }

        private static void OnReverseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReportInputsAndJsonPanel self)
            {
self.ApplyReverse((bool)e.NewValue);
          }
        }

        private void ApplyReverse(bool reverse)
   {
            // True Grid layout: swap main column (0) with json column (4)
            // Note: With Grid-based rows, we need to swap all elements in column 0 and column 4
            var json = this.FindName("txtCurrentJson") as UIElement;
   if (json == null) return;

  // Get all children in column 0 (main content) and move to column 4, and vice versa
            var grid = this.Content as Grid;
       if (grid == null) return;

  if (reverse)
   {
                // json column 0, main content to column 4
         Grid.SetColumn(json, 0);
    // Swap main column content
          foreach (UIElement child in grid.Children)
   {
    if (child == json) continue;
   var col = Grid.GetColumn(child);
             if (col == 0) Grid.SetColumn(child, 4);
 else if (col == 4) Grid.SetColumn(child, 0);
            }
            }
       else
            {
          // main content column 0, json to column 4
          Grid.SetColumn(json, 4);
// Restore main column content
    foreach (UIElement child in grid.Children)
              {
        if (child == json) continue;
     var col = Grid.GetColumn(child);
        if (col == 4 && Grid.GetColumnSpan(child) == 1) Grid.SetColumn(child, 0);
       else if (col == 0 && Grid.GetRow(child) != 0) continue; // Leave column 0 items alone
       }
            }
        }

        private void SetupAltArrowNavigation()
        {
   // Find textboxes that may be inside templates/panels
            var studyRemark = FindName("txtStudyRemark") as TextBox;
       var patientRemark = FindName("txtPatientRemark") as TextBox;
            
 if (studyRemark == null || patientRemark == null)
         {
      System.Diagnostics.Debug.WriteLine("[ReportInputsAndJsonPanel] Could not find txtStudyRemark or txtPatientRemark");
  return;
}

 // Existing navigation pairs
            SetupAltArrowPair(studyRemark, txtChiefComplaint, Key.Down, Key.Up);
            SetupAltArrowPair(txtChiefComplaint, txtChiefComplaintProofread, Key.Right, Key.Left);

        // NEW: Additional vertical navigation through the form
       // Study Remark -> Chief Complaint (already exists above)
            // Chief Complaint Proofread -> Study Remark
         SetupOneWayAltArrow(txtChiefComplaintProofread, studyRemark, Key.Up);
        
   // Chief Complaint -> Patient Remark
          SetupOneWayAltArrow(txtChiefComplaint, patientRemark, Key.Down);
       
    // Chief Complaint Proofread -> Patient Remark
          SetupOneWayAltArrow(txtChiefComplaintProofread, patientRemark, Key.Down);
  
   // Patient Remark -> Chief Complaint
            SetupOneWayAltArrow(patientRemark, txtChiefComplaint, Key.Up);
            
  // Patient Remark -> Patient History
            SetupOneWayAltArrow(patientRemark, txtPatientHistory, Key.Down);
            
   // Patient History -> Patient Remark
     SetupOneWayAltArrow(txtPatientHistory, patientRemark, Key.Up);
 
   // Patient History Proofread -> Patient Remark
        SetupOneWayAltArrow(txtPatientHistoryProofread, patientRemark, Key.Up);
 
     // Patient History <-> Patient History Proofread (horizontal)
  SetupAltArrowPair(txtPatientHistory, txtPatientHistoryProofread, Key.Right, Key.Left);
      
  // NEW: Navigation from Patient History to EditorFindings (if TargetEditor is set)
       if (TargetEditor != null)
            {
         SetupTextBoxToEditorNavigation(txtPatientHistory, TargetEditor, Key.Down);
    SetupTextBoxToEditorNavigation(txtPatientHistoryProofread, TargetEditor, Key.Down);
                SetupEditorToTextBoxNavigation(TargetEditor, txtPatientHistory, Key.Up);
}
  }

        private void SetupAltArrowPair(TextBox source, TextBox target, Key sourceKey, Key targetKey)
        {
         // Source -> Target navigation
       source.PreviewKeyDown += (s, e) =>
 {
              // When Alt is pressed, arrow keys are reported as SystemKey
       var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
   
  if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == sourceKey)
     {
              HandleAltArrowNavigation(source, target);
               e.Handled = true;
     }
            };

       // Target -> Source navigation
     target.PreviewKeyDown += (s, e) =>
            {
                // When Alt is pressed, arrow keys are reported as SystemKey
           var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
  
          if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == targetKey)
     {
       HandleAltArrowNavigation(target, source);
          e.Handled = true;
     }
        };
        }

        private void SetupOneWayAltArrow(TextBox source, TextBox target, Key key)
        {
 source.PreviewKeyDown += (s, e) =>
      {
     var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
    
          if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == key)
       {
   HandleAltArrowNavigation(source, target);
           e.Handled = true;
            }
            };
   }

        private void SetupTextBoxToEditorNavigation(TextBox source, EditorControl target, Key key)
        {
    source.PreviewKeyDown += (s, e) =>
            {
    var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
                
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == key)
          {
 HandleTextBoxToEditorNavigation(source, target);
       e.Handled = true;
    }
            };
        }

        private void SetupEditorToTextBoxNavigation(EditorControl source, TextBox target, Key key)
      {
      // Find the underlying MusmEditor (AvalonEdit TextEditor)
            var musmEditor = source.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
    if (musmEditor == null) return;

    musmEditor.PreviewKeyDown += (s, e) =>
    {
       var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
       
   if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == key)
       {
        HandleEditorToTextBoxNavigation(source, target);
        e.Handled = true;
        }
      };
     }

        private void HandleAltArrowNavigation(TextBox source, TextBox target)
{
            if (string.IsNullOrEmpty(source.SelectedText))
            {
// No selection: just move focus
         target.Focus();
      target.CaretIndex = target.Text?.Length ?? 0;
            }
 else
     {
         // Has selection: copy to end of target and move focus
    var selectedText = source.SelectedText;
       var targetText = target.Text ?? string.Empty;
  
        // Append selected text to target (with newline if target is not empty)
       if (!string.IsNullOrEmpty(targetText))
     {
        target.Text = targetText + "\n" + selectedText;
           }
                else
 {
    target.Text = selectedText;
         }
      
         // Move focus to target and position caret at end
     target.Focus();
                target.CaretIndex = target.Text.Length;
          }
        }

        private void HandleTextBoxToEditorNavigation(TextBox source, EditorControl target)
        {
            var musmEditor = target.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
            if (musmEditor == null) return;

       if (string.IsNullOrEmpty(source.SelectedText))
        {
        // No selection: just move focus
  musmEditor.Focus();
 musmEditor.CaretOffset = musmEditor.Text?.Length ?? 0;
            }
else
       {
   // Has selection: copy to end of target
    var selectedText = source.SelectedText;
          var targetText = musmEditor.Text ?? string.Empty;
        
  if (!string.IsNullOrEmpty(targetText))
         {
         musmEditor.Text = targetText + "\n" + selectedText;
       }
else
      {
   musmEditor.Text = selectedText;
       }
    
       musmEditor.Focus();
              musmEditor.CaretOffset = musmEditor.Text.Length;
 }
 }

        private void HandleEditorToTextBoxNavigation(EditorControl source, TextBox target)
        {
var musmEditor = source.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
 if (musmEditor == null) return;

   if (string.IsNullOrEmpty(musmEditor.SelectedText))
            {
            // No selection: just move focus
          target.Focus();
             target.CaretIndex = target.Text?.Length ?? 0;
          }
            else
            {
                // Has selection: copy to end of target
   var selectedText = musmEditor.SelectedText;
          var targetText = target.Text ?? string.Empty;
      
      if (!string.IsNullOrEmpty(targetText))
     {
          target.Text = targetText + "\n" + selectedText;
}
           else
     {
   target.Text = selectedText;
                }
         
      target.Focus();
     target.CaretIndex = target.Text.Length;
      }
        }
    }
}
