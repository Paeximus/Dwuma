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

    public JobsController(JoobleJobService joobleJobService)
    {
        _joobleJobService = joobleJobService;
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
}
