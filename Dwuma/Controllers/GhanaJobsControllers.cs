using Dwuma.Models.Jobs;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/jobs/ghana")]
[Produces("application/json")]
public sealed class GhanaJobsController : ControllerBase
{
    private readonly CareerjetJobService _careerjetJobService;

    public GhanaJobsController(
        CareerjetJobService careerjetJobService)
    {
        _careerjetJobService =
            careerjetJobService;
    }

    [HttpGet]
    public async Task<ActionResult<GhanaJobSearchResponse>> Search(
        [FromQuery] GhanaJobSearchRequest request,
        CancellationToken cancellationToken)
    {
        string userIp =
            HttpContext.Connection.RemoteIpAddress
                ?.MapToIPv4()
                .ToString()
            ?? "127.0.0.1";

                if (userIp == "127.0.0.1")
                {
                    userIp = "8.8.8.8";
                }

        string userAgent =
            Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            userAgent = "Dwuma/1.0";
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