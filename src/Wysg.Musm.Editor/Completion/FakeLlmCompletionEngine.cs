using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wysg.Musm.Editor.Completion;

public sealed class FakeLlmCompletionEngine : ICompletionEngine
{
    public async IAsyncEnumerable<string> StreamAsync(CompletionRequest req,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var s = Choose(req.LeftContext);
        foreach (var ch in s)
        {
            ct.ThrowIfCancellationRequested();
            yield return ch.ToString();
            await Task.Delay(60, ct);
        }
    }
    private static string Choose(string left)
    {
        var l = left.ToLowerInvariant();
        if (l.Contains("cta")) return "No flow-limiting stenosis or aneurysm identified.";
        if (l.Contains("dwi") || l.Contains("diffusion")) return "Findings are compatible with acute infarction.";
        if (l.Contains("no acute")) return " intracranial hemorrhage identified.";
        return "No acute intracranial hemorrhage, territorial infarct, or mass effect.";
    }
}
