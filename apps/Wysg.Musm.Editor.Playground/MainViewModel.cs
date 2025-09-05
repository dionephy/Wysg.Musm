using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Playground.Completion;
using Wysg.Musm.Editor.Playground.SampleData;
using Wysg.Musm.Editor.Snippets;

namespace Wysg.Musm.Editor.Playground;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private string _reportBody = "Ghost uses GhostWords; popup uses PopupWords.\nType: th …\n";
    public string ReportBody { get => _reportBody; set { _reportBody = value; Raise(); } }

    private bool _aiEnabled = true;
    public bool AiEnabled { get => _aiEnabled; set { _aiEnabled = value; Raise(); } }

    // Popup completion from PopupWords; add your InMemorySnippetProvider if you want both
    public ISnippetProvider SnippetProvider { get; } =
        new WordListCompletionProvider(PopupWords.Words);

    // Ghost from GhostWords
    public ICompletionEngine CompletionEngine { get; } =
        new PrefixGhostEngine(GhostWords.Words);
}
