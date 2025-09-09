using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.GhostApi.Domain;
using Wysg.Musm.GhostApi.Orchestration;

namespace Wysg.Musm.GhostApi.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class GhostController : ControllerBase
{
    private readonly SuggestionOrchestrator _orchestrator;

    public GhostController(SuggestionOrchestrator orchestrator) => _orchestrator = orchestrator;

    [HttpPost("suggest")]
    public async Task<ActionResult<SuggestResponse>> Suggest([FromBody] SuggestRequest req, CancellationToken ct)
        => Ok(await _orchestrator.SuggestAsync(req, ct));
}
