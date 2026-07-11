using Dwuma.Models.Data.DwumaContext;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/job-interactions")]
public sealed class JobInteractionsController : ControllerBase
{
    private readonly JobInteractionService _interactionService;

    public JobInteractionsController(
        JobInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    [HttpPost("{jobId:int}/click")]
    public async Task<IActionResult> Click(
        int jobId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        await _interactionService.RecordClickAsync(
            userId,
            jobId,
            cancellationToken);

        return Ok(new { message = "Click recorded." });
    }

    [HttpPost("{jobId:int}/save")]
    public async Task<IActionResult> Save(
        int jobId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        await _interactionService.RecordSaveAsync(
            userId,
            jobId,
            cancellationToken);

        return Ok(new { message = "Save recorded." });
    }

    [HttpPost("{jobId:int}/dismiss")]
    public async Task<IActionResult> Dismiss(
        int jobId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        await _interactionService.RecordDismissAsync(
            userId,
            jobId,
            cancellationToken);

        return Ok(new { message = "Dismiss recorded." });
    }

    

}

