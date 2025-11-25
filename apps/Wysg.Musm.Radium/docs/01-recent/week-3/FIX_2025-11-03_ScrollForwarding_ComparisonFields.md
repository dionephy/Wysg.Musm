# Fix: Scroll Forwarding for Comparison Fields (2025-11-03)

Summary
- Problem: Scrolling over comparison fields (RichTextBox/DiffTextBox) locked the page scroll. Wheel events were consumed by inner controls and never reached the outer ScrollViewer.
- Solution: Added a root-level PreviewMouseWheel interceptor that:
  - Walks up to the nearest TextBoxBase (TextBox, RichTextBox, DiffTextBox)
  - Checks boundary of inner ScrollViewer (top/bottom)
  - Forwards wheel to nearest outer ScrollViewer using ScrollToVerticalOffset when at a boundary
  - Uses safe parent traversal to handle non-visual ancestors (FlowDocument)
- Also kept per-TextBox handlers as a fallback for standard TextBoxes.

Files changed
- apps/Wysg.Musm.Radium/Controls/ReportInputsAndJsonPanel.xaml.cs
  - Root wheel interceptor + safe parent traversal + scrollable ancestor search
  - Detailed logging for diagnostics
- apps/Wysg.Musm.Radium/Controls/PreviousReportTextAndJsonPanel.xaml.cs
  - Root wheel interceptor + safe parent traversal
  - Comments and logging

Design details
- Boundary-aware forwarding
  - Only forward when inner editor cannot scroll further in the wheel direction
  - Prevents stealing wheel while the inner control can still scroll itself
- Safe parent traversal
  - Replaces direct VisualTreeHelper.GetParent
  - Handles Visual, Visual3D, FrameworkContentElement, ContentElement, and logical parents
- Scroll target selection
  - Prefers the nearest ancestor ScrollViewer with ScrollableHeight > 0
  - Falls back to any scrollable viewer within the control as last resort
- Diagnostics
  - Logs origin, editor type, inner offsets, forward decision, outer offsets (before/after)

How to verify
1) Hover over comparison field and scroll
2) Expect the outer panel to scroll when the comparison box is at its top or bottom
3) Output window shows lines like:
   - [ReportInputsAndJsonPanel] ROOT Wheel from '' type=RichTextBox delta=-120, inner=off=0/max=0, forward=True, outer=yes(off=240/max=980), e.Handled=False
   - [ReportInputsAndJsonPanel] OUTER scrolled: 240 -> 288 (max=980)

Notes
- This logic is robust across WPF TextBox and RichTextBox-based controls.
- The forwarding step size is ~48px per notch; tune if needed.
