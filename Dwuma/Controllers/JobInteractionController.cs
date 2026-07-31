using Dwuma.Models.Data.DwumaContext;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;
using Dwuma.Extensions;
using Microsoft.AspNetCore.Authorization;
namespace Dwuma.Controllers;

[ApiController]
[Authorize]
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
    CancellationToken cancellationToken)
    {
        int userId = User.GetUserId();

        await _interactionService.RecordClickAsync(
            userId,
            jobId,
            cancellationToken);

        return Ok(new
        {
            message = "Job click recorded."
        });
    }

    [HttpPost("{jobId:int}/save")]
    public async Task<IActionResult> Save(
    int jobId,
    CancellationToken cancellationToken)
    {
        int userId = User.GetUserId();

        await _interactionService.RecordSaveAsync(
            userId,
            jobId,
            cancellationToken);

        return Ok(new
        {
            message = "Job saved."
        });
    }
    [HttpPost("{jobId:int}/dismiss")]
    public async Task<IActionResult> Dismiss(
    int jobId,
    CancellationToken cancellationToken)
    {
        int userId = User.GetUserId();

        await _interactionService.RecordDismissAsync(
            userId,
            jobId,
            cancellationToken);

        return Ok(new
        {
            message = "Job dismissed."
        });
    }



}

