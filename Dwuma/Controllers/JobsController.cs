using Dwuma.Models.Jobs;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly JobSearchService _jobSearchService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        JobSearchService jobSearchService,
        ILogger<JobsController> logger)
    {
        _jobSearchService = jobSearchService;
        _logger = logger;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
    [FromQuery] string? query,
    [FromQuery] string? location,
    [FromQuery] int page = 1,
    [FromQuery] bool? remoteOnly = null,
    [FromQuery] bool englishOnly = false,
    CancellationToken cancellationToken = default)
    {
        try
        {
            JobSearchResponse result =
                await _jobSearchService.SearchAsync(
                    query,
                    location,
                    page,
                    remoteOnly,
                    englishOnly,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Job search failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The external job provider is currently unavailable."
                });
        }
    }
}