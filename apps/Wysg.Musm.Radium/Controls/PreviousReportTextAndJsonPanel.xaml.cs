using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Wysg.Musm.Radium.Controls
{
    /// <summary>
    /// Reusable panel displaying Previous Report's header_and_findings text alongside JSON view.
    /// Used in both side (landscape) and bottom (portrait) panels to reduce duplication.
    /// </summary>
    public partial class PreviousReportTextAndJsonPanel : UserControl
    {
        // Dependency Property for Header and Findings Text
        public static readonly DependencyProperty HeaderAndFindingsTextProperty =
            DependencyProperty.Register(
                nameof(HeaderAndFindingsText),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string HeaderAndFindingsText
        {
            get => (string)GetValue(HeaderAndFindingsTextProperty);
            set => SetValue(HeaderAndFindingsTextProperty, value);
        }

        // Dependency Property for Final Conclusion Text
        public static readonly DependencyProperty FinalConclusionTextProperty =
            DependencyProperty.Register(
                nameof(FinalConclusionText),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string FinalConclusionText
        {
            get => (string)GetValue(FinalConclusionTextProperty);
            set => SetValue(FinalConclusionTextProperty, value);
        }

        // Dependency Property for JSON Text
        public static readonly DependencyProperty JsonTextProperty =
            DependencyProperty.Register(
                nameof(JsonText),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string JsonText
        {
            get => (string)GetValue(JsonTextProperty);
            set => SetValue(JsonTextProperty, value);
        }

        // Split outputs and header temp
        public static readonly DependencyProperty HeaderTempProperty =
            DependencyProperty.Register(
                nameof(HeaderTemp),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string HeaderTemp { get => (string)GetValue(HeaderTempProperty); set => SetValue(HeaderTempProperty, value); }

        public static readonly DependencyProperty SplitFindingsProperty =
            DependencyProperty.Register(
                nameof(SplitFindings),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string SplitFindings { get => (string)GetValue(SplitFindingsProperty); set => SetValue(SplitFindingsProperty, value); }

        public static readonly DependencyProperty SplitConclusionProperty =
            DependencyProperty.Register(
                nameof(SplitConclusion),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string SplitConclusion { get => (string)GetValue(SplitConclusionProperty); set => SetValue(SplitConclusionProperty, value); }

        // Proofread fields
        public static readonly DependencyProperty FindingsProofreadProperty =
            DependencyProperty.Register(
                nameof(FindingsProofread),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string FindingsProofread { get => (string)GetValue(FindingsProofreadProperty); set => SetValue(FindingsProofreadProperty, value); }

        public static readonly DependencyProperty ConclusionProofreadProperty =
            DependencyProperty.Register(
                nameof(ConclusionProofread),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string ConclusionProofread { get => (string)GetValue(ConclusionProofreadProperty); set => SetValue(ConclusionProofreadProperty, value); }

        // NEW: IsJsonCollapsed dependency property with default value true
        public static readonly DependencyProperty IsJsonCollapsedProperty =
            DependencyProperty.Register(
                nameof(IsJsonCollapsed),
                typeof(bool),
                typeof(PreviousReportTextAndJsonPanel),
                new PropertyMetadata(true, OnIsJsonCollapsedChanged));

        public bool IsJsonCollapsed
        {
            get => (bool)GetValue(IsJsonCollapsedProperty);
            set => SetValue(IsJsonCollapsedProperty, value);
        }

        private static void OnIsJsonCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviousReportTextAndJsonPanel self && e.NewValue is bool collapsed)
            {
                self.UpdateJsonColumnVisibility(collapsed);
            }
        }

        private void UpdateJsonColumnVisibility(bool collapsed)
        {
            if (JsonSplitter == null || txtJson == null || btnToggleJson == null)
                return;

            if (collapsed)
            {
                // Collapsed: just hide the TextBox and splitter, leave column structure intact
                JsonSplitter.Visibility = Visibility.Collapsed;
                txtJson.Visibility = Visibility.Collapsed;
                btnToggleJson.Content = "\u25B6"; // Right arrow (expand)
            }
            else
            {
                // Expanded: show the TextBox and splitter
                JsonSplitter.Visibility = Visibility.Visible;
                txtJson.Visibility = Visibility.Visible;
                btnToggleJson.Content = "\u25C0"; // Left arrow (collapse)
            }
        }

        // Dependency Property for Reverse layout (optional, for future use)
        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(
                nameof(Reverse),
                typeof(bool),
                typeof(PreviousReportTextAndJsonPanel),
                new PropertyMetadata(false, OnReverseChanged));

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }

        private static void OnReverseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviousReportTextAndJsonPanel panel)
            {
                panel.ApplyReverse((bool)e.NewValue);
            }
        }

        public PreviousReportTextAndJsonPanel()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                Debug.WriteLine("[PreviousReportTextAndJsonPanel] Loaded: initializing layout + scroll fixes");
                ApplyReverse(Reverse);
                // Apply default collapsed state after control is loaded
                UpdateJsonColumnVisibility(IsJsonCollapsed);
                // Legacy per-TextBox hook and robust root interceptor
                AttachMouseWheelScrollFix2();
                AttachRootWheelInterceptor();
            };
        }

        private void ApplyReverse(bool reverse)
        {
            // 5-column layout: 0=textboxes, 1=splitter, 2=proofread, 3=splitter, 4=json
            var scrollViewer = this.FindName("PART_LeftHost") as UIElement; // left textboxes panel
            var jsonBox = this.FindName("txtJson") as UIElement;            // right json panel

            if (scrollViewer == null || jsonBox == null) return;

            if (reverse)
            {
                // json | splitter | proofread | splitter | textboxes
                Grid.SetColumn(jsonBox, 0);
                Grid.SetColumn(scrollViewer, 4);
            }
            else
            {
                // textboxes | splitter | proofread | splitter | json
                Grid.SetColumn(scrollViewer, 0);
                Grid.SetColumn(jsonBox, 4);
            }
        }

        /// <summary>
        /// Safe parent traversal for Visual/Visual3D and ContentElement hierarchies.
        /// Avoids InvalidOperationException from VisualTreeHelper.GetParent on non-visuals.
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
                var logical = ContentOperations.GetParent(ce);
                if (logical != null) return logical;
            }
            return LogicalTreeHelper.GetParent(child);
        }

        /// <summary>
        /// Finds the nearest parent of type T using safe traversal.
        /// </summary>
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

        /// <summary>
        /// Attach a root-level wheel interceptor so we can forward wheel at boundaries even if handled.
        /// </summary>
        private void AttachRootWheelInterceptor()
        {
            AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnRootPreviewMouseWheel), true);
            Debug.WriteLine("[PreviousReportTextAndJsonPanel] Root wheel interceptor attached (handledEventsToo=true)");
        }

        /// <summary>
        /// Root wheel handler for previous-report panel. Finds nearest TextBoxBase (TextBox/RichTextBox), checks inner boundary,
        /// then forwards to the nearest outer ScrollViewer when needed.
        /// </summary>
        private void OnRootPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject src) return;

            TextBoxBase? editor = null;
            var cur = src;
            while (cur != null)
            {
                if (cur is TextBoxBase tbBase) { editor = tbBase; break; }
                cur = GetParentObject(cur);
            }
            if (editor == null)
            {
                Debug.WriteLine($"[PreviousReportTextAndJsonPanel] ROOT Wheel: no TextBoxBase ancestor. Source={src.GetType().FullName}");
                return;
            }

            var innerSv = FindVisualChild<ScrollViewer>(editor);
            bool forward;
            if (innerSv == null || innerSv.ScrollableHeight <= 0)
            {
                forward = true;
            }
            else if (e.Delta > 0)
            {
                forward = innerSv.VerticalOffset <= 0;
            }
            else
            {
                forward = innerSv.VerticalOffset >= innerSv.ScrollableHeight;
            }

            var outerSv = FindScrollableAncestor(editor);
            Debug.WriteLine($"[PreviousReportTextAndJsonPanel] ROOT Wheel from '{editor.Name}' type={editor.GetType().Name} delta={e.Delta}, inner={(innerSv!=null ? $"off={innerSv.VerticalOffset:F0}/max={innerSv.ScrollableHeight:F0}" : "null")}, forward={forward}, outer={(outerSv!=null ? $"yes(off={outerSv.VerticalOffset:F0}/max={outerSv.ScrollableHeight:F0})" : "no")}, e.Handled={e.Handled}");

            if (!forward || outerSv == null) return;

            e.Handled = true;
            int steps = System.Math.Max(1, System.Math.Abs(e.Delta) / 120);
            double deltaOffset = steps * 48; // ~3 lines per notch
            double before = outerSv.VerticalOffset;
            if (e.Delta < 0)
                outerSv.ScrollToVerticalOffset(System.Math.Min(outerSv.VerticalOffset + deltaOffset, outerSv.ScrollableHeight));
            else
                outerSv.ScrollToVerticalOffset(System.Math.Max(outerSv.VerticalOffset - deltaOffset, 0));
            double after = outerSv.VerticalOffset;
            Debug.WriteLine($"[PreviousReportTextAndJsonPanel] OUTER scrolled: {before:F0} -> {after:F0} (max={outerSv.ScrollableHeight:F0})");
        }

        /// <summary>
        /// Finds the nearest ancestor ScrollViewer which can scroll (ScrollableHeight > 0).
        /// Falls back to any ScrollViewer within this control.
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
                    best ??= sv;
                }
                if (ReferenceEquals(cur, this)) break;
            }

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

        /// <summary>
        /// Finds a visual child of type T via DFS.
        /// </summary>
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

        /// <summary>
        /// Attach per-TextBox wheel handler. Root interceptor supersedes most cases but we keep this for redundancy.
        /// </summary>
        private void AttachMouseWheelScrollFix2()
        {
            int count = 0;
            foreach (var tb in FindVisualChildren<TextBox>(this))
            {
                tb.PreviewMouseWheel -= OnChildPreviewMouseWheel2;
                tb.PreviewMouseWheel += OnChildPreviewMouseWheel2;
                count++;
            }
            Debug.WriteLine($"[PreviousReportTextAndJsonPanel] Attached wheel handler to {count} TextBox controls (v2)");
        }

        /// <summary>
        /// Per-TextBox wheel handler with boundary-aware forwarding to the nearest outer ScrollViewer.
        /// </summary>
        private void OnChildPreviewMouseWheel2(object sender, MouseWheelEventArgs e)
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
            var outerSv = FindScrollableAncestor(tb);
            Debug.WriteLine($"[PreviousReportTextAndJsonPanel] Wheel on TB '{tb.Name}' delta={e.Delta}, innerSv={(innerSv!=null ? $"off={innerSv.VerticalOffset:F0}/max={innerSv.ScrollableHeight:F0}" : "null")}, forward={forward}, outerSv={(outerSv!=null ? "yes" : "no")}");
            if (!forward || outerSv == null) return;
            e.Handled = true;
            int steps = System.Math.Max(1, System.Math.Abs(e.Delta) / 120);
            for (int i = 0; i < steps; i++)
            {
                if (e.Delta < 0) outerSv.LineDown(); else outerSv.LineUp();
            }
        }

        /// <summary>
        /// Enumerate all descendants of type T.
        /// </summary>
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
