using Dwuma.Models.JobMatching;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/job-match")]
public sealed class JobMatchController : ControllerBase
{
    private readonly JobMatchService _jobMatchService;
    private readonly ILogger<JobMatchController> _logger;

    public JobMatchController(
        JobMatchService jobMatchService,
        ILogger<JobMatchController> logger)
    {
        _jobMatchService = jobMatchService;
        _logger = logger;
    }

    [HttpPost("analyse")]
    [ProducesResponseType(
        typeof(JobMatchResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Analyse(
        [FromBody] JobMatchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            JobMatchResponse response =
                await _jobMatchService.AnalyseAsync(
                    request,
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Job compatibility analysis failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The job compatibility service is currently unavailable."
                });
        }
    }
}