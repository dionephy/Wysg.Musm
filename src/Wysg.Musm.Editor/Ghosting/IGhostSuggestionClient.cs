using System.Threading;
using System.Threading.Tasks;

namespace Wysg.Musm.Editor.Ghosting;

public interface IGhostSuggestionClient
{
    Task<GhostResponse> SuggestAsync(GhostRequest request, CancellationToken ct);
}
