using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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
                // Initialize layout and wire up input handlers once the visual tree is ready.
                Debug.WriteLine("[ReportInputsAndJsonPanel] Loaded: initializing layout + scroll fixes");
                ApplyReverse(Reverse);
                SetupAltArrowNavigation();
                // JSON panel is now always visible - no need to update visibility

                // Attach per-TextBox wheel handler (legacy) and a root-level interceptor (robust).
                // Root interceptor is required to catch wheel events when inner controls already mark them handled
                // and to support non-TextBox editors (RichTextBox / DiffTextBox) used by comparison fields.
                AttachMouseWheelScrollFix();
                AttachRootWheelInterceptor();
            };
        }

        /// <summary>
        /// Hooks a root-level PreviewMouseWheel handler and listens with handledEventsToo=true.
        /// This ensures we can forward wheel events even if inner controls already handled them.
        /// </summary>
        private void AttachRootWheelInterceptor()
        {
            AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnRootPreviewMouseWheel), true);
            Debug.WriteLine("[ReportInputsAndJsonPanel] Root wheel interceptor attached (handledEventsToo=true)");
        }

        /// <summary>
        /// Root-level wheel handler that:
        /// 1) Finds the nearest TextBoxBase (covers TextBox, RichTextBox, DiffTextBox)
        /// 2) Checks if the inner editor can scroll further in the requested direction
        /// 3) If at boundary, forwards the wheel to the nearest outer ScrollViewer
        ///    using ScrollToVerticalOffset for smooth deterministic motion.
        /// Safe parent traversal is used to handle ContentElement/FlowDocument ancestors.
        /// </summary>
        private void OnRootPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject src) return;

            // Walk up to a TextBoxBase to support TextBox and RichTextBox-based editors.
            TextBoxBase? editor = null;
            var cur = src;
            while (cur != null)
            {
                if (cur is TextBoxBase tbBase) { editor = tbBase; break; }
                cur = GetParentObject(cur);
            }

            if (editor == null)
            {
                Debug.WriteLine($"[ReportInputsAndJsonPanel] ROOT Wheel: no TextBoxBase ancestor. Source={src.GetType().FullName}");
                return;
            }

            // IMPORTANT: Don't forward scroll events from JSON TextBox (txtCurrentJson)
            // The JSON column scrolls independently and should not affect the left columns
            if (editor.Name == "txtCurrentJson")
            {
                Debug.WriteLine($"[ReportInputsAndJsonPanel] ROOT Wheel: Ignoring JSON TextBox scroll (independent column)");
                return;
            }

            // Determine whether to forward based on the inner ScrollViewer boundary.
            var innerSv = FindVisualChild<ScrollViewer>(editor);
            bool forward;
            if (innerSv == null || innerSv.ScrollableHeight <= 0)
            {
                // No inner scroll present (or not scrollable) -> forward to outer viewer
                forward = true;
            }
            else if (e.Delta > 0)
            {
                // Wheel up: forward when already at top
                forward = innerSv.VerticalOffset <= 0;
            }
            else
            {
                // Wheel down: forward when already at bottom
                forward = innerSv.VerticalOffset >= innerSv.ScrollableHeight;
            }

            // Resolve the nearest outer ScrollViewer that can actually scroll
            var outerSv = FindScrollableAncestor(editor);
            Debug.WriteLine($"[ReportInputsAndJsonPanel] ROOT Wheel from '{editor.Name}' type={editor.GetType().Name} delta={e.Delta}, inner={(innerSv!=null ? $"off={innerSv.VerticalOffset:F0}/max={innerSv.ScrollableHeight:F0}" : "null")}, forward={forward}, outer={(outerSv!=null ? $"yes(off={outerSv.VerticalOffset:F0}/max={outerSv.ScrollableHeight:F0})" : "no")}, e.Handled={e.Handled}");

            if (!forward || outerSv == null) return;

            // Perform the actual forwarding (one notch ~= 120 delta)
            e.Handled = true;
            int steps = System.Math.Max(1, System.Math.Abs(e.Delta) / 120);
            double deltaOffset = steps * 48; // Adjust scroll distance per step (~3 text lines)
            double before = outerSv.VerticalOffset;
            if (e.Delta < 0)
                outerSv.ScrollToVerticalOffset(System.Math.Min(outerSv.VerticalOffset + deltaOffset, outerSv.ScrollableHeight));
            else
                outerSv.ScrollToVerticalOffset(System.Math.Max(outerSv.VerticalOffset - deltaOffset, 0));
            double after = outerSv.VerticalOffset;
            Debug.WriteLine($"[ReportInputsAndJsonPanel] OUTER scrolled: {before:F0} -> {after:F0} (max={outerSv.ScrollableHeight:F0})");
        }

        /// <summary>
        /// Finds the nearest ancestor ScrollViewer which can scroll (ScrollableHeight &gt; 0).
        /// Falls back to any ScrollViewer under this control to ensure wheel can still move the page.
        /// </summary>
        private ScrollViewer? FindScrollableAncestor(DependencyObject start)
        {
            ScrollViewer? best = null;
            var cur = start;
            while (cur != null)
            {
                cur = GetParentObject(cur);
                if (cur is ScrollViewer sv)
                {
                    if (sv.ScrollableHeight > 0) return sv;
                    best ??= sv; // Remember first viewer as fallback
                }

                if (ReferenceEquals(cur, this)) break; // stop at control boundary
            }

            // Fallback: find any scrollable viewer beneath this control
            if (best == null || best.ScrollableHeight <= 0)
            {
                foreach (var sv in FindVisualChildren<ScrollViewer>(this))
                {
                    if (sv.ScrollableHeight > 0) return sv;
                    best ??= sv;
                }
            }
            return best;
        }

        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(nameof(Reverse), typeof(bool), typeof(ReportInputsAndJsonPanel), new PropertyMetadata(false, OnReverseChanged));

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }

        // IsJsonCollapsed property removed - JSON panel is always visible
        // Legacy property kept for backward compatibility but has no effect
        public static readonly DependencyProperty IsJsonCollapsedProperty =
            DependencyProperty.Register(nameof(IsJsonCollapsed), typeof(bool), typeof(ReportInputsAndJsonPanel), 
                new PropertyMetadata(false)); // Changed default to false (not collapsed)

        public bool IsJsonCollapsed
        {
            get => (bool)GetValue(IsJsonCollapsedProperty);
            set => SetValue(IsJsonCollapsedProperty, value);
        }

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

        // NEW: Additional vertical navigation through the form
       // Study Remark -> Chief Complaint (already exists above)
            // Chief Complaint -> Patient Remark
         SetupOneWayAltArrow(txtChiefComplaint, patientRemark, Key.Down);
        
   // Patient Remark -> Chief Complaint
          SetupOneWayAltArrow(patientRemark, txtChiefComplaint, Key.Up);
          
   // Patient Remark -> Patient History
     SetupOneWayAltArrow(patientRemark, txtPatientHistory, Key.Down);
     
   // Patient History -> Patient Remark
     SetupOneWayAltArrow(txtPatientHistory, patientRemark, Key.Up);
 
     // NEW: Navigation from Patient History to EditorFindings (if TargetEditor is set)
       if (TargetEditor != null)
            {
         SetupTextBoxToEditorNavigation(txtPatientHistory, TargetEditor, Key.Down);
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

        private void AttachMouseWheelScrollFix()
        {
            int count = 0;
            foreach (var tb in FindVisualChildren<TextBox>(this))
            {
                tb.PreviewMouseWheel -= OnChildPreviewMouseWheel;
                tb.PreviewMouseWheel += OnChildPreviewMouseWheel;
                count++;
            }
            Debug.WriteLine($"[ReportInputsAndJsonPanel] Attached wheel handler to {count} TextBox controls");
        }

        private void OnChildPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var innerSv = FindVisualChild<ScrollViewer>(tb);
            bool forward;
            if (innerSv == null || innerSv.ScrollableHeight <= 0)
            {
                forward = true;
            }
            else if (e.Delta > 0)
            {
                forward = innerSv.VerticalOffset <= 0; // up at top
            }
            else
            {
                forward = innerSv.VerticalOffset >= innerSv.ScrollableHeight; // down at bottom
            }

            var outerSv = GetOuterScrollViewer(tb);
            Debug.WriteLine($"[ReportInputsAndJsonPanel] Wheel on TB '{tb.Name}' delta={e.Delta}, innerSv={(innerSv!=null ? $"off={innerSv.VerticalOffset:F0}/max={innerSv.ScrollableHeight:F0}" : "null")}, forward={forward}, outerSv={(outerSv!=null ? "yes" : "no")}");

            if (!forward || outerSv == null) return; // Let inner handle

            e.Handled = true;
            int steps = System.Math.Max(1, System.Math.Abs(e.Delta) / 120);
            for (int i = 0; i < steps; i++)
            {
                if (e.Delta < 0) outerSv.LineDown(); else outerSv.LineUp();
            }
        }

        private static ScrollViewer? GetOuterScrollViewer(DependencyObject start)
        {
            DependencyObject? current = start;
            ScrollViewer? firstSv = null;
            while (current != null)
            {
                current = GetParentObject(current);
                if (current is ScrollViewer sv)
                {
                    if (firstSv == null)
                    {
                        firstSv = sv;
                        if (!string.Equals(sv.Name, "PART_ContentHost")) return sv;
                        continue;
                    }
                    return sv;
                }
            }
            return null;
        }

        /// <summary>
        /// SAFE parent lookup that supports non-Visual objects (e.g., FlowDocument)
        /// </summary>
        private static DependencyObject? GetParentObject(DependencyObject? child)
        {
            if (child == null) return null;

            if (child is Visual || child is Visual3D)
                return VisualTreeHelper.GetParent(child);

            if (child is FrameworkContentElement fce)
                return fce.Parent;

            if (child is ContentElement ce)
            {
                var logicalParent = ContentOperations.GetParent(ce);
                if (logicalParent != null) return logicalParent;
            }

            return LogicalTreeHelper.GetParent(child);
        }

        private static T? FindVisualParent<T>(DependencyObject? obj) where T : DependencyObject
        {
            var current = obj;
            while (current != null)
            {
                var parent = GetParentObject(current);
                if (parent is T t) return t;
                current = parent;
            }
            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject? obj) where T : DependencyObject
        {
            if (obj == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var sub = FindVisualChild<T>(child);
                if (sub != null) return sub;
            }
            return null;
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) yield return t;
                foreach (var sub in FindVisualChildren<T>(child)) yield return sub;
            }
        }
    }
}
