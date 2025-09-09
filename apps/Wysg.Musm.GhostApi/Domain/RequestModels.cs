namespace Wysg.Musm.GhostApi.Domain;

public sealed record SuggestRequest(
    string ReportText,
    string PatientSex,
    int PatientAge,
    StudyHeader StudyHeader,
    StudyInfo StudyInfo,
    int TopK = 3,
    int MaxPerLine = 1,
    int LatencyBudgetMs = 800,
    string? StudyHeaderText = null  // free text header (optional)
);

public sealed record StudyHeader(string? Clinical, string? PriorStudyDate);
public sealed record StudyInfo(string[] StudyName, string StudyDate);
