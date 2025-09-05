using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

/// <summary>
/// Minimal option picker for placeholder choices, bound to a TextArea.
/// For SingleChoice: pick once and close.
/// For MultiSelect: stays open; each selection invokes the callback (caller appends/merges).
/// </summary>
public sealed class PlaceholderCompletionWindow : CompletionWindow
{
    private readonly bool _multi;
    private readonly Action<string> _onPick;

    public PlaceholderCompletionWindow(TextArea textArea,
        IEnumerable<(string key, string value)> options,
        bool multiSelect,
        Action<string> onPick)
        : base(textArea ?? throw new ArgumentNullException(nameof(textArea)))
    {
        _multi = multiSelect;
        _onPick = onPick ?? throw new ArgumentNullException(nameof(onPick));

        CloseAutomatically = !_multi;          // keep open for multi-select
        CompletionList.IsFiltering = true;

        SizeToContent = SizeToContent.Height;
        MaxHeight = 280; Width = 360;

        var data = CompletionList.CompletionData;
        foreach (var (k, v) in options)
            data.Add(new OptionItem(k, v, _onPick, () => { if (!_multi) Close(); }));

        // Optional: use icon from Themes/Generic.xaml if present
        try
        {
            if (Application.Current?.TryFindResource("HotkeyIcon") is ImageSource img)
                CompletionList.ListBox.Resources["ItemIcon"] = img;
        }
        catch { }
    }

    private sealed class OptionItem : ICompletionData
    {
        private readonly string _key;
        private readonly string _value;
        private readonly Action<string> _onPick;
        private readonly Action _onClose;

        public OptionItem(string key, string value, Action<string> onPick, Action onClose)
        {
            _key = key; _value = value; _onPick = onPick; _onClose = onClose;
        }

        public ImageSource? Image => null;
        public string Text => _key;                           // filter by key
        public object Content => $"{_key} — {_value}";
        public object Description => _value;
        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            _onPick(_value);
            _onClose();
        }
    }
}
