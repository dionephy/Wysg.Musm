using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Snippets;

namespace Wysg.Musm.Editor.Playground;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private string _reportBody = "Start typing here…\nTry: nq + Ctrl+Space\n";
    public string ReportBody { get => _reportBody; set { _reportBody = value; Raise(); } }

    private bool _aiEnabled = true;
    public bool AiEnabled { get => _aiEnabled; set { _aiEnabled = value; Raise(); } }

    // Use your real provider/engine in production
    public ISnippetProvider SnippetProvider { get; } = new InMemorySnippetProvider();
    public ICompletionEngine CompletionEngine { get; } = new FakeLlmCompletionEngine();
}
