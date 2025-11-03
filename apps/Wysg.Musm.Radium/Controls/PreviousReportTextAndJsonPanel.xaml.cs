using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
                // Hook scroll forwarding so wheel works over inner textboxes
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

        // SAFE parent lookup that supports non-Visual objects (e.g., FlowDocument)
        private static DependencyObject? GetParentObject(DependencyObject? child)
        {
            if (child == null) return null;
            if (child is Visual || child is Visual3D) return VisualTreeHelper.GetParent(child);
            if (child is FrameworkContentElement fce) return fce.Parent;
            if (child is ContentElement ce)
            {
                var logical = ContentOperations.GetParent(ce);
                if (logical != null) return logical;
            }
            return LogicalTreeHelper.GetParent(child);
        }

        private void AttachRootWheelInterceptor()
        {
            AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnRootPreviewMouseWheel), true);
            Debug.WriteLine("[PreviousReportTextAndJsonPanel] Root wheel interceptor attached (handledEventsToo=true)");
        }

        private void OnRootPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject src) return;
            // Walk up through visual/logical parents to find the nearest TextBox
            TextBox? tb = null;
            var cur = src;
            while (cur != null)
            {
                if (cur is TextBox t) { tb = t; break; }
                cur = GetParentObject(cur);
            }
            if (tb == null) return;

            var innerSv = FindVisualChild<ScrollViewer>(tb);
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

            var outerSv = GetOuterScrollViewer(tb);
            Debug.WriteLine($"[PreviousReportTextAndJsonPanel] ROOT Wheel from TB '{tb.Name}' delta={e.Delta}, inner={(innerSv!=null ? $"off={innerSv.VerticalOffset:F0}/max={innerSv.ScrollableHeight:F0}" : "null")}, forward={forward}, outer={(outerSv!=null ? "yes" : "no")}, e.Handled={e.Handled}");

            if (!forward || outerSv == null) return;

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

        // v2 methods only (remove old duplicates)
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
            var outerSv = GetOuterScrollViewer(tb);
            Debug.WriteLine($"[PreviousReportTextAndJsonPanel] Wheel on TB '{tb.Name}' delta={e.Delta}, innerSv={(innerSv!=null ? $"off={innerSv.VerticalOffset:F0}/max={innerSv.ScrollableHeight:F0}" : "null")}, forward={forward}, outerSv={(outerSv!=null ? "yes" : "no")}");
            if (!forward || outerSv == null) return;
            e.Handled = true;
            int steps = System.Math.Max(1, System.Math.Abs(e.Delta) / 120);
            for (int i = 0; i < steps; i++)
            {
                if (e.Delta < 0) outerSv.LineDown(); else outerSv.LineUp();
            }
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
