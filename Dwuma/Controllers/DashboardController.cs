using Dwuma.Models.Dashboard;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dwuma.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(
        DashboardService dashboardService)
    {
        _dashboardService =
            dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(DashboardResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardResponse>>
        GetDashboard(
            CancellationToken cancellationToken)
    {
        int userId =
            GetUserId();

        DashboardResponse response =
            await _dashboardService
                .GetDashboardAsync(
                    userId,
                    cancellationToken);

        return Ok(response);
    }

    private int GetUserId()
    {
        string? value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                value,
                out int userId))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user could not be identified.");
        }

        return userId;
    }
}