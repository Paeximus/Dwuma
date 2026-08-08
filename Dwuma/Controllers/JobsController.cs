using Dwuma.Models.Data.DwumaContext;
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
    private readonly SerpApiJobService _serpApiJobService;

    public JobsController(
        JoobleJobService joobleJobService,
        CareerjetJobService careerjetJobService,
        SerpApiJobService serpApiJobService)
    {
        _joobleJobService = joobleJobService;
        _careerjetJobService = careerjetJobService;
        _serpApiJobService = serpApiJobService;
    }

    // =========================================================
    // MAIN JOB SEARCH - SERPAPI
    // =========================================================

    [AllowAnonymous]
    [HttpGet("search")]
    [ProducesResponseType(
    typeof(JobSearchResult),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobSearchResult>> Search(
    [FromQuery] string? query = "jobs",
    [FromQuery] string? location = "Accra, Ghana",
    [FromQuery] string? jobType = null,
    [FromQuery] bool? remote = null,
    [FromQuery] string? nextPageToken = null,
    CancellationToken cancellationToken = default)
    {
        string searchQuery =
            string.IsNullOrWhiteSpace(query)
                ? "jobs"
                : query.Trim();

        string searchLocation =
            string.IsNullOrWhiteSpace(location)
                ? "Accra, Ghana"
                : location.Trim();

        JobSearchResult result =
            await _serpApiJobService
                .SearchJobsWithPaginationAsync(
                    searchQuery,
                    searchLocation,
                    jobType,
                    remote,
                    nextPageToken,
                    cancellationToken);

        return Ok(result);
    }


    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
    typeof(JobListing),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobListing>> GetJobById(
    int id,
    CancellationToken cancellationToken = default)
    {
        JobListing? job =
            await _serpApiJobService.GetJobByIdAsync(
                id,
                cancellationToken);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        return Ok(job);
    }

    // =========================================================
    // JOOBLE 
    // =========================================================

    //[AllowAnonymous]
    //[HttpGet("jooble")]
    //[ProducesResponseType(
    //    typeof(GhanaJobSearchResponse),
    //    StatusCodes.Status200OK)]
    //public async Task<ActionResult<GhanaJobSearchResponse>>
    //    SearchJooble(
    //        [FromQuery] string? query = "jobs",
    //        [FromQuery] string? location = "Ghana",
    //        [FromQuery] int page = 1,
    //        [FromQuery] int pageSize = 20,
    //        CancellationToken cancellationToken = default)
    //{
    //    var request =
    //        new GhanaJobSearchRequest
    //        {
    //            Keywords = query,
    //            Location = location,
    //            Page = page,
    //            PageSize = pageSize,
    //            CompanySearch = false
    //        };

    //    GhanaJobSearchResponse result =
    //        await _joobleJobService.SearchAsync(
    //            request,
    //            cancellationToken);

    //    return Ok(result);
    //}

    // =========================================================
    // GHANA JOOBLE SEARCH 
    // =========================================================

    //[AllowAnonymous]
    //[HttpGet("ghana")]
    //[ProducesResponseType(
    //    typeof(GhanaJobSearchResponse),
    //    StatusCodes.Status200OK)]
    //public async Task<ActionResult<GhanaJobSearchResponse>> Ghana(
    //    [FromQuery] string? keywords = "jobs",
    //    [FromQuery] string? location = "Ghana",
    //    [FromQuery] int page = 1,
    //    [FromQuery] int pageSize = 20,
    //    CancellationToken cancellationToken = default)
    //{
    //    var request =
    //        new GhanaJobSearchRequest
    //        {
    //            Keywords = keywords,
    //            Location = location,
    //            Page = page,
    //            PageSize = pageSize
    //        };

    //    GhanaJobSearchResponse result =
    //        await _joobleJobService.SearchAsync(
    //            request,
    //            cancellationToken);

    //    return Ok(result);
    //}

    // =========================================================
    // CACHED DATABASE JOBS
    // =========================================================

    [HttpPost("cached")]
    public async Task<ActionResult<GhanaJobSearchResponse>>
        GetCachedJobs(
            [FromBody] GhanaJobSearchRequest request,
            CancellationToken cancellationToken)
    {
        GhanaJobSearchResponse result =
            await _joobleJobService
                .GetCachedJobsAsync(
                    request,
                    cancellationToken);

        return Ok(result);
    }

    // =========================================================
    // CAREERJET 
    // =========================================================

    //[AllowAnonymous]
    //[HttpGet("careerjet")]
    //public async Task<ActionResult<GhanaJobSearchResponse>>
    //    SearchCareerjet(
    //        [FromQuery] string? query = "jobs",
    //        [FromQuery] string? location = "Ghana",
    //        [FromQuery] int page = 1,
    //        [FromQuery] int pageSize = 20,
    //        CancellationToken cancellationToken = default)
    //{
    //    var request =
    //        new GhanaJobSearchRequest
    //        {
    //            Keywords = query,
    //            Location = location,
    //            Page = page,
    //            PageSize = pageSize,
    //            CompanySearch = false
    //        };

    //    string userIp =
    //        HttpContext.Connection.RemoteIpAddress?
    //            .MapToIPv4()
    //            .ToString()
    //        ?? "127.0.0.1";

    //    string userAgent =
    //        Request.Headers.UserAgent.ToString();

    //    if (string.IsNullOrWhiteSpace(
    //            userAgent))
    //    {
    //        userAgent =
    //            "DWUMA-Web-App";
    //    }

    //    GhanaJobSearchResponse result =
    //        await _careerjetJobService.SearchAsync(
    //            request,
    //            userIp,
    //            userAgent,
    //            cancellationToken);

    //    return Ok(result);
    //}
}