using Dwuma.Models.Jobs;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly JoobleJobService _joobleJobService;
    private readonly CareerjetJobService _careerjetJobService;

    public JobsController(
    JoobleJobService joobleJobService,
    CareerjetJobService careerjetJobService)
    {
        _joobleJobService = joobleJobService;
        _careerjetJobService = careerjetJobService;
    }

    [AllowAnonymous]
    [HttpGet("search")]
    [ProducesResponseType(typeof(GhanaJobSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GhanaJobSearchResponse>> Search(
        [FromQuery] string? query = "jobs",
        [FromQuery] string? location = "Ghana",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool companySearch = false,
        CancellationToken cancellationToken = default)
    {
        var request = new GhanaJobSearchRequest
        {
            Keywords = query,
            Location = location,
            Page = page,
            PageSize = pageSize,
            CompanySearch = companySearch
        };

        GhanaJobSearchResponse result =
            await _joobleJobService.SearchAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("ghana")]
    [ProducesResponseType(typeof(GhanaJobSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GhanaJobSearchResponse>> Ghana(
        [FromQuery] string? keywords = "jobs",
        [FromQuery] string? location = "Ghana",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var request = new GhanaJobSearchRequest
        {
            Keywords = keywords,
            Location = location,
            Page = page,
            PageSize = pageSize
        };

        return Ok(await _joobleJobService.SearchAsync(
            request,
            cancellationToken));
    }

    [HttpPost("cached")]
    public async Task<ActionResult<GhanaJobSearchResponse>>
    GetCachedJobs(
        [FromBody] GhanaJobSearchRequest request,
        CancellationToken cancellationToken)
    {
        GhanaJobSearchResponse result =
            await _joobleJobService.GetCachedJobsAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("careerjet")]
    public async Task<ActionResult<GhanaJobSearchResponse>>
    SearchCareerjet(
        [FromQuery] string? query = "jobs",
        [FromQuery] string? location = "Ghana",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var request = new GhanaJobSearchRequest
        {
            Keywords = query,
            Location = location,
            Page = page,
            PageSize = pageSize,
            CompanySearch = false
        };

        string userIp =
            HttpContext.Connection.RemoteIpAddress?
                .MapToIPv4()
                .ToString()
            ?? "127.0.0.1";

        string userAgent =
            Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            userAgent = "DWUMA-Web-App";
        }

        GhanaJobSearchResponse result =
            await _careerjetJobService.SearchAsync(
                request,
                userIp,
                userAgent,
                cancellationToken);

        return Ok(result);
    }
}
