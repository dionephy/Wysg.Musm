using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.Register(
            nameof(SelectCommand), typeof(ICommand), typeof(PreviousStudiesStrip));
        public ICommand? SelectCommand
        {
            get => (ICommand?)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }

        public static readonly DependencyProperty AddCommandProperty = DependencyProperty.Register(
            nameof(AddCommand), typeof(ICommand), typeof(PreviousStudiesStrip));
        public ICommand? AddCommand
        {
            get => (ICommand?)GetValue(AddCommandProperty);
            set => SetValue(AddCommandProperty, value);
        }

        private readonly ObservableCollection<object> _visible = new();
        private readonly ObservableCollection<object> _overflow = new();

        private void RefreshLayout()
        {
            // Order items by StudyDateTime desc if present
            var items = (ItemsSource ?? Enumerable.Empty<object>())
                .Cast<object>()
                .OrderByDescending(o => (o.GetType().GetProperty("StudyDateTime")?.GetValue(o) as DateTime?) ?? DateTime.MinValue)
                .ToList();

            _visible.Clear();
            _overflow.Clear();

            // Bind to UI elements
            icVisible.ItemsSource = _visible;

            // Measure available width: Root.ActualWidth - Add width - reserved space (AddWidth for overflow + padding)
            if (double.IsNaN(Root.ActualWidth) || Root.ActualWidth <= 0) return;

            double addWidth = btnAdd.ActualWidth > 0 ? btnAdd.ActualWidth : btnAdd.MinWidth;
            double reserve = addWidth * 2; // space for overflow button and padding
            double available = Math.Max(0, Root.ActualWidth - reserve);

            // Create a temp button to measure text width
            var measurer = new ToggleButton { Margin = new Thickness(4, 0, 0, 0) };
            var style = TryFindResource("DarkToggleButtonStyle") as Style ?? Application.Current.TryFindResource("DarkToggleButtonStyle") as Style;
            if (style != null) measurer.Style = style;

            double used = 0;
            foreach (var it in items)
            {
                var title = it.GetType().GetProperty("Title")?.GetValue(it) as string ?? it.ToString() ?? string.Empty;
                measurer.Content = title;
                measurer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double w = measurer.DesiredSize.Width;

                if (used + w <= available)
                {
                    _visible.Add(it);
                    used += w;
                }
                else
                {
                    _overflow.Add(it);
                }
            }

            btnOverflow.Visibility = _overflow.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnOverflowClick(object sender, RoutedEventArgs e)
        {
            if (_overflow.Count == 0) return;
            var ctx = new ContextMenu();
            foreach (var it in _overflow)
            {
                var mi = new MenuItem { Header = GetHeader(it), IsCheckable = true };
                if (GetIsSelected(it))
                    mi.IsChecked = true;
                mi.Click += (_, __) => SelectCommand?.Execute(it);
                ctx.Items.Add(mi);
            }
            ctx.PlacementTarget = btnOverflow;
            ctx.IsOpen = true;
        }

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

        private static object GetHeader(object it)
        {
            var title = it.GetType().GetProperty("Title")?.GetValue(it) as string;
            return title ?? it.ToString() ?? string.Empty;
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (PreviousStudiesStrip)d;
            if (e.OldValue is INotifyCollectionChanged oldObs)
                oldObs.CollectionChanged -= ctl.OnCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newObs)
                newObs.CollectionChanged += ctl.OnCollectionChanged;
            ctl.RefreshLayout();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshLayout();
        }
    }
}
