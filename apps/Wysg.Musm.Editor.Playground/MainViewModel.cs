using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wysg.Musm.Editor.Ghosting;
using Wysg.Musm.Editor.Playground.Completion;
using Wysg.Musm.Editor.Snippets;


namespace Wysg.Musm.Editor.Playground;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private string _reportBody =
@"mild microangiopathy

old infarction in right frontal lobe

no other abnormality";


    public ISnippetProvider SnippetProvider { get; } =
        new CompositeSnippetProvider()
            .AddTokens("thalamus", "hydrocephalus", "infarction")
            .AddHotkey("dba", "diffuse brain atrophy")
            .AddSnippet(new CodeSnippet(
                "mnsd",
                "mild nasal septal deviation",
                "mild nasal septal deviation to the ${1^laterality=1^right|3^left}"
            ));

    // (your ghost client bindings unchanged)
    public string ReportBody { get => _reportBody; set { _reportBody = value; Raise(); } }

    public string PatientSex { get; set; } = "M";
    public int PatientAge { get; set; } = 72;
    public string StudyHeader { get; set; } = "Follow-up MRI Brain with DWI and SWI";
    public string StudyInfo { get; set; } = "Hx HTN, DM; dizziness";

    public IGhostSuggestionClient GhostClient { get; } = new FakeGhostClient();

    

}
