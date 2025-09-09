// src/Wysg.Musm.Editor/Snippets/PlaceholderCompletionWindow.cs
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

public sealed class PlaceholderCompletionWindow : Window
{
    private readonly TextArea _area;
    private readonly Border _chrome;
    private readonly ListBox _list;
    private bool _isMulti;

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
            MinWidth = 200,
            MaxWidth = 380,
            MaxHeight = 240,
            Background = Brushes.White,
            Foreground = Brushes.Black,
            FontSize = 12,
            SnapsToDevicePixels = true,
            Focusable = false,
            SelectionMode = SelectionMode.Single
        };
        _list.DisplayMemberPath = nameof(Item.Display);

        _chrome = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = _list,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                ShadowDepth = 2,
                BlurRadius = 8,
                Opacity = 0.25
            },
            Focusable = false
        };

        Content = _chrome;
        Deactivated += (_, __) => Close();
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
