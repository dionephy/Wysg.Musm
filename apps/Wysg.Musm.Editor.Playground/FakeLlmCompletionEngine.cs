using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wysg.Musm.Editor.Completion;

namespace Wysg.Musm.Editor.Playground;

public sealed class FakeLlmCompletionEngine : ICompletionEngine
{
    public async IAsyncEnumerable<string> StreamAsync(CompletionRequest req,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Tiny dev stub that streams a plausible completion, slowly
        var s = Choose(req.LeftContext);
        foreach (var ch in s)
        {
            ct.ThrowIfCancellationRequested();
            yield return ch.ToString();
            await Task.Delay(45, ct);
        }
    }

    private static string Choose(string left)
    {
        var l = left.ToLowerInvariant();
        if (l.Contains("cta")) return " No flow-limiting stenosis or aneurysm identified.";
        if (l.Contains("dwi") || l.Contains("diffusion")) return " Findings are compatible with acute infarction.";
        if (l.Contains("no acute")) return " intracranial hemorrhage identified.";
        return " No acute intracranial hemorrhage, territorial infarct, or mass effect.";
    }
}
