// src/Wysg.Musm.Editor/Snippets/PlaceholderCompletionWindow.cs
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

/// NOTE (Root cause and fix):
/// Previously, this popup attempted to forward a synthetic Tab key (via InputManager.ProcessInput)
/// to the editor when a user pressed Tab or clicked an item. That created re-entrant input routing
/// while the popup was still handling its own input event. Combined with caret changes and other
/// completion windows closing on caret moves, this caused InvalidOperationException crashes
/// (closing popups while still in their own input handlers).
///
/// Fix:
/// - Do NOT synthesize Tab. Instead, raise a CommitRequested event that SnippetInputHandler subscribes to.
/// - Defer raising CommitRequested using Dispatcher.BeginInvoke so the current input event unwinds first.
/// - Close the window asynchronously on Deactivated to avoid re-entrant close during input processing.
/// This event-driven, deferred approach removes the re-entrancy and stabilizes Tab/mouse commit even when
/// the popup (or other popups) manipulate caret/selection concurrently.

public sealed class PlaceholderCompletionWindow : Window
{
    private readonly TextArea _area;
    private readonly Border _chrome;
    private readonly ListBox _list;
    private bool _isMulti;

    public event System.EventHandler<Item?>? CommitRequested;

    public sealed class Item
    {
        public string Key { get; }
        public string Text { get; }
        public bool IsChecked { get; set; } // used for multi-choice visual
        public string Display => (IsChecked ? "✓ " : "") + $"{Key}: {Text}";
        public Item(string key, string text, bool isChecked = false)
        {
            Key = key; Text = text; IsChecked = isChecked;
        }
        public override string ToString() => Display;
    }

    public PlaceholderCompletionWindow(TextArea area, IEnumerable<Item> items, bool isMulti)
    {
        _area = area;
        _isMulti = isMulti;

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Focusable = false;
        ResizeMode = ResizeMode.NoResize;

        _list = new ListBox
        {
            ItemsSource = items.ToList(),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 4, 6, 4),
            MinWidth = 220,
            MaxWidth = 420,
            MaxHeight = 260,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            FontSize = 12,
            SnapsToDevicePixels = true,
            Focusable = false,
            SelectionMode = SelectionMode.Single
        };
        _list.DisplayMemberPath = nameof(Item.Display);

        // Intercept Tab/Space via event handlers (even though we don't take focus)
        _list.PreviewKeyDown += OnListPreviewKeyDown;
        // Mouse commit: behave like Tab for single-choice (Mode 1/3)
        _list.PreviewMouseLeftButtonUp += OnListPreviewMouseLeftButtonUp;

        // Dark border + subtle shadow
        _chrome = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(65, 65, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = _list,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                ShadowDepth = 2,
                BlurRadius = 8,
                Opacity = 0.35
            },
            Focusable = false
        };

        Content = _chrome;
        // Close asynchronously on deactivation to avoid re-entrant input/close crashes
        Deactivated += (_, __) =>
        {
            System.Diagnostics.Debug.WriteLine("[PH] Window Deactivated → schedule Close()");
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                try { Close(); System.Diagnostics.Debug.WriteLine("[PH] Window Closed"); }
                catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[PH] Close error: {ex.Message}"); }
            }), System.Windows.Threading.DispatcherPriority.Background);
        };
        PreviewMouseDown += OnWindowPreviewMouseDown;
        PreviewMouseUp += OnWindowPreviewMouseUp;
    }

    private void OnListPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            // Toggle selection in multi-choice context only; do not insert a space into editor
            e.Handled = true;
            if (_isMulti)
            {
                ToggleCurrent();
            }
        }
    }

    private void OnListPreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[PH] List.PreviewMouseLeftButtonUp");
        // Ensure the clicked item becomes selected
        if (e.OriginalSource is DependencyObject dep)
        {
            var container = FindAncestor<ListBoxItem>(dep);
            if (container != null)
            {
                _list.SelectedItem = container.DataContext;
                _list.ScrollIntoView(_list.SelectedItem);
                if (_list.SelectedItem is Item it)
                    System.Diagnostics.Debug.WriteLine($"[PH] List item select via click: {it.Key}:{it.Text}");
            }
        }

        // For single-choice (Mode 1/3), treat mouse click as Tab (commit and complete)
        if (!_isMulti)
        {
            e.Handled = true;
            var sel = _list.SelectedItem as Item;
            // Defer commit to allow mouse event to unwind before popup is closed
            System.Diagnostics.Debug.WriteLine($"[PH] CommitRequested via Mouse; sel={(sel!=null?sel.Text:"(null)")}");
            Dispatcher.BeginInvoke(new System.Action(() => CommitRequested?.Invoke(this, sel)), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void OnWindowPreviewMouseDown(object? sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        System.Diagnostics.Debug.WriteLine($"[PH] Window.PreviewMouseDown btn={e.ChangedButton} pos={pos}");
    }

    private void OnWindowPreviewMouseUp(object? sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        System.Diagnostics.Debug.WriteLine($"[PH] Window.PreviewMouseUp btn={e.ChangedButton} pos={pos}");
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null && current is not T)
            current = VisualTreeHelper.GetParent(current);
        return current as T;
    }

    public void ShowAtCaret()
    {
        var tv = _area.TextView;
        var vp = tv.GetVisualPosition(_area.Caret.Position, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.TextBottom);
        var screen = _area.PointToScreen(new Point(vp.X, vp.Y));

        Left = screen.X + 6;
        Top = screen.Y + 6;

        _chrome.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Width = _chrome.DesiredSize.Width;
        Height = _chrome.DesiredSize.Height;

        if (_list.Items.Count > 0 && _list.SelectedIndex < 0)
            _list.SelectedIndex = 0;

        Show();
    }

    public void SelectFirst()
    {
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
    }

    public void MoveSelection(int delta)
    {
        if (_list.Items.Count == 0) return;
        int idx = _list.SelectedIndex >= 0 ? _list.SelectedIndex : 0;
        idx = System.Math.Clamp(idx + delta, 0, _list.Items.Count - 1);
        _list.SelectedIndex = idx;
        _list.ScrollIntoView(_list.SelectedItem);
    }

    public void SelectByKey(string key)
    {
        var found = _list.Items.Cast<Item>().FirstOrDefault(i => i.Key.Equals(key, System.StringComparison.OrdinalIgnoreCase));
        if (found is null) return;
        _list.SelectedItem = found;
        _list.ScrollIntoView(found);
    }

    public void ToggleCurrent()
    {
        if (!_isMulti) return;
        if (_list.SelectedItem is Item it)
        {
            it.IsChecked = !it.IsChecked;
            // refresh
            var items = _list.Items.Cast<Item>().ToList();
            _list.ItemsSource = null;
            _list.ItemsSource = items;
            _list.SelectedItem = it;
            _list.ScrollIntoView(it);
        }
    }

    public Item? Selected => _list.SelectedItem as Item;

    public List<string> GetSelectedTexts()
    {
        return _list.Items.Cast<Item>().Where(i => i.IsChecked).Select(i => i.Text).ToList();
    }

    public void SetItems(IEnumerable<Item> items, bool isMulti)
    {
        _isMulti = isMulti;
        var list = items.ToList();
        _list.ItemsSource = list;
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        _list.InvalidateVisual();
    }
}
