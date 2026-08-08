using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Jobs;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class SerpApiTestController : ControllerBase
{
    private readonly SerpApiJobService _serpApiJobService;

    public SerpApiTestController(
        SerpApiJobService serpApiJobService)
    {
        _serpApiJobService = serpApiJobService;
    }

    [HttpGet("serpapi-test")]
    public async Task<ActionResult<List<JobListing>>> TestSerpApi(
        [FromQuery] string query = "software engineer",
        [FromQuery] string location = "Accra, Ghana",
        CancellationToken cancellationToken = default)
    {
        List<JobListing> result =
            await _serpApiJobService.SearchAndCacheJobsAsync(
                query,
                location,
                cancellationToken);

        return Ok(result);
    }
}