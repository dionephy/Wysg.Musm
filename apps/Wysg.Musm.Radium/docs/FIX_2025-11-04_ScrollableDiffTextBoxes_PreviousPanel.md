# Enhancement: Scrollable Diff TextBoxes in Previous Report Panel (2025-11-04)

Summary
- Applied scroll-forwarding behavior to all diff textboxes and rich diff viewers in `PreviousReportTextAndJsonPanel`.
- Hovering and scrolling now works naturally: when the inner diff viewer reaches its top/bottom, the outer panel scrolls.
- Matches behavior implemented earlier for current report panel.

Changes
- apps/Wysg.Musm.Radium/Controls/PreviousReportTextAndJsonPanel.xaml.cs
  - Added root-level `PreviewMouseWheel` interceptor that:
    - Locates nearest `TextBoxBase` ancestor (covers `TextBox` and `RichTextBox`-based editors like diff viewers)
    - Detects inner boundary via the inner `ScrollViewer` (top/bottom)
    - Forwards the wheel to the nearest scrollable outer `ScrollViewer` using `ScrollToVerticalOffset`
  - Safe parent traversal (`GetParentObject`) to support non-visual ancestors (e.g., `FlowDocument`)
  - Utility `FindScrollableAncestor` for selecting the best outer scroll target
  - Kept per-TextBox handler as redundancy
- No XAML changes required; works with existing `SideBySideDiffViewer` and textboxes.

Design Notes
- Uses `TextBoxBase` to include `RichTextBox` (diff viewers) along with `TextBox`.
- Only forwards when the inner editor cannot scroll further in the requested direction (prevents stealing when inner can still scroll).
- Uses safe tree walking to avoid `InvalidOperationException` with non-visual nodes.

How to Verify
1) Open a previous study, expand the diff viewers.
2) Hover over a diff pane and scroll.
3) When the pane is at boundary, the outer panel should continue scrolling smoothly.

Build Status
- Build successful with no errors.

Related Docs
- FIX_2025-11-03_ScrollForwarding_ComparisonFields.md (original design for current report panel)
