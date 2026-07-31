using Dwuma.Models.Authentication;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Dwuma.Extensions;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthResponse response =
                await _authService.RegisterAsync(
                    request,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Registration conflict for {Email}.",
                request.Email);

            return Conflict(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Registration failed for {Email}.",
                request.Email);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = ex.Message
                });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthResponse response =
                await _authService.LoginAsync(
                    request,
                    cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "Failed login attempt for {Email}.",
                request.Email);

            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        int userId = User.GetUserId();

        string? fullName =
            User.FindFirstValue(
                ClaimTypes.Name);

        string? email =
            User.FindFirstValue(
                ClaimTypes.Email);

        return Ok(new
        {
            userId,
            fullName,
            email
        });
    }

}