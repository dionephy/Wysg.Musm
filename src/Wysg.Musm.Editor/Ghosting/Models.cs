namespace Wysg.Musm.Editor.Ghosting;

public sealed record GhostRequest(
    string ReportText,
    string PatientSex,
    int PatientAge,
    string StudyHeader,
    string StudyInfo
);

public sealed record GhostSuggestion(int LineNumber, string Suggestion);

public sealed record GhostResponse(IReadOnlyList<GhostSuggestion> Suggestions);
