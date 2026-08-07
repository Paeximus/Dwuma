using Dwuma.Extensions;
using Dwuma.Models.Auth;
using Dwuma.Models.Authentication;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

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


    [EnableRateLimiting("auth-policy")]
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
            RegisterResponse response =
                await _authService.RegisterAsync
                (
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
    }



    [EnableRateLimiting("auth-policy")]
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

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
    [FromBody] VerifyEmailRequest request,
    CancellationToken cancellationToken)
    {
        await _authService.VerifyEmailAsync(
            request,
            cancellationToken);

        return Ok(new
        {
            message =
                "Email verified successfully.",

            nextRoute =
                "/onboarding/step-1"
        });
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
    [FromBody] ResendVerificationRequest request,
    CancellationToken cancellationToken)
    {
        await _authService
            .ResendVerificationEmailAsync(
                request.Email,
                cancellationToken);

        return Ok(new
        {
            message =
                "If the account exists and is not verified, a new verification email has been sent."
        });
    }
}