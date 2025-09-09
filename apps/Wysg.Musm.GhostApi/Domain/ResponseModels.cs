namespace Wysg.Musm.GhostApi.Domain;

public sealed record SuggestResponse(
    List<LineSuggestion> Lines,
    ModelInfo Model
);

public sealed record LineSuggestion(int LineIndex, string Ghost, double Confidence, string Source);
public sealed record ModelInfo(string Name, string Quant);
