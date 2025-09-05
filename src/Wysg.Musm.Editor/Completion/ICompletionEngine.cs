using System.Collections.Generic;
using System.Threading;

namespace Wysg.Musm.Editor.Completion;

public sealed record CompletionRequest(string LeftContext, string RightContext, int MaxTokens);
public interface ICompletionEngine
{
    IAsyncEnumerable<string> StreamAsync(CompletionRequest req, CancellationToken ct);
}
