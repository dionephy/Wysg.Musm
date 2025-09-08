// src/Wysg.Musm.Editor/Snippets/PlaceholderCompletionWindow.cs
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

public sealed class PlaceholderCompletionWindow : Window
{
    private readonly ListBox _list;
    private readonly TextArea _area;

    public record Item(int Digit, string Text);

    public PlaceholderCompletionWindow(TextArea area, System.Collections.Generic.IEnumerable<Item> items)
    {
        _area = area;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        _list = new ListBox
        {
            ItemsSource = items.ToList(),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            MinWidth = 160
        };
        Content = _list;
        Deactivated += (_, __) => Close();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override void OnContentRendered(System.EventArgs e)
    {
        base.OnContentRendered(e);
        _list.Focus();
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (_list.SelectedItem is not Item it) return;

        if (e.Key == Key.Enter)
        {
            DialogResult = true;
            Close();
            return;
        }
        // digit direct-select
        if (e.Key >= Key.D0 && e.Key <= Key.D9)
        {
            var d = (int)(e.Key - Key.D0);
            var found = _list.Items.Cast<Item>().FirstOrDefault(x => x.Digit == d);
            if (found is not null)
            {
                _list.SelectedItem = found;
                DialogResult = true;
                Close();
            }
            e.Handled = true;
        }
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    public Item? Selected => _list.SelectedItem as Item;

    public void ShowAtCaret()
    {
        var vp = _area.TextView.GetVisualPosition(_area.Caret.Position, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.TextBottom);
        var p = _area.PointToScreen(new Point(vp.X, vp.Y));
        Left = p.X; Top = p.Y;
        Show();
    }
}
