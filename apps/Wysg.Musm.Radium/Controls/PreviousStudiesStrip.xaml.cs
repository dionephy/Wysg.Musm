using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Wysg.Musm.Radium.Controls
{
    public partial class PreviousStudiesStrip : UserControl
    {
        public PreviousStudiesStrip()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshLayout();
            SizeChanged += (_, __) => RefreshLayout();
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(PreviousStudiesStrip), new PropertyMetadata(null, OnItemsChanged));
        public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

        public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.Register(
            nameof(SelectCommand), typeof(ICommand), typeof(PreviousStudiesStrip));
        public ICommand? SelectCommand { get => (ICommand?)GetValue(SelectCommandProperty); set => SetValue(SelectCommandProperty, value); }

        public static readonly DependencyProperty AddCommandProperty = DependencyProperty.Register(
            nameof(AddCommand), typeof(ICommand), typeof(PreviousStudiesStrip));
        public ICommand? AddCommand { get => (ICommand?)GetValue(AddCommandProperty); set => SetValue(AddCommandProperty, value); }

        private readonly ObservableCollection<object> _visible = new();
        private readonly ObservableCollection<object> _overflow = new();

        private static bool GetIsSelected(object it)
        {
            var prop = it.GetType().GetProperty("IsSelected");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                var value = prop.GetValue(it);
                if (value is bool b) return b;
            }
            return false;
        }
        private static void SetIsSelected(object it, bool value)
        {
            var prop = it.GetType().GetProperty("IsSelected");
            if (prop != null && prop.PropertyType == typeof(bool)) prop.SetValue(it, value);
        }

        private static object GetStudyDateTime(object it)
            => it.GetType().GetProperty("StudyDateTime")?.GetValue(it) ?? DateTime.MinValue;

        private void RefreshLayout()
        {
            var items = (ItemsSource ?? Enumerable.Empty<object>()).Cast<object>()
                .OrderByDescending(o => (DateTime)(GetStudyDateTime(o) ?? DateTime.MinValue))
                .ToList();
            _visible.Clear(); _overflow.Clear(); icVisible.ItemsSource = _visible;
            if (double.IsNaN(Root.ActualWidth) || Root.ActualWidth <= 0) return;
            double addWidth = btnAdd.ActualWidth > 0 ? btnAdd.ActualWidth : btnAdd.MinWidth;
            double reserve = addWidth * 2;
            double available = Math.Max(0, Root.ActualWidth - reserve);
            var measurer = new ToggleButton { Margin = new Thickness(4, 0, 0, 0) };
            var style = TryFindResource("DarkToggleButtonStyle") as Style ?? Application.Current.TryFindResource("DarkToggleButtonStyle") as Style;
            if (style != null) measurer.Style = style;
            double used = 0;
            foreach (var it in items)
            {
                var title = it.GetType().GetProperty("Title")?.GetValue(it) as string ?? it.ToString() ?? string.Empty;
                measurer.Content = title; measurer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double w = measurer.DesiredSize.Width;
                if (used + w <= available) { _visible.Add(it); used += w; }
                else _overflow.Add(it);
            }
            btnOverflow.Visibility = _overflow.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnOverflowClick(object sender, RoutedEventArgs e)
        {
            if (_overflow.Count == 0) return;
            var ctx = new ContextMenu();
            foreach (var it in _overflow)
            {
                var mi = new MenuItem { Header = it.GetType().GetProperty("Title")?.GetValue(it) ?? it.ToString(), IsCheckable = true };
                mi.IsChecked = GetIsSelected(it);
                mi.Click += (_, __) => SelectCommand?.Execute(it);
                ctx.Items.Add(mi);
            }
            ctx.PlacementTarget = btnOverflow; ctx.IsOpen = true;
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (PreviousStudiesStrip)d;
            if (e.OldValue is INotifyCollectionChanged oldObs) oldObs.CollectionChanged -= ctl.OnCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newObs) newObs.CollectionChanged += ctl.OnCollectionChanged;
            ctl.RefreshLayout();
        }
        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshLayout();

        // Prevent toggle-off of the active tab (unless selection changes to another)
        private void OnTabPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ToggleButton tb && tb.DataContext is object ctx)
            {
                if (GetIsSelected(ctx))
                {
                    // Already selected: block unselect action
                    e.Handled = true; // swallow, leaving IsChecked=true
                    return;
                }
                // Not selected: allow selection via command
                SelectCommand?.Execute(ctx);
                e.Handled = true; // prevent default toggle behavior (we manage IsSelected)
            }
        }
    }
}
