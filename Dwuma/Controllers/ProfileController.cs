using System.ComponentModel.DataAnnotations;
using Dwuma.Extensions;
using Dwuma.Models;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

/// <summary>
/// Manages the authenticated user's profile and CV documents.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ProfileService profileService,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Creates or updates the authenticated user's profile.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(ProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrUpdate(
        [FromBody] ProfileRequest request,
        CancellationToken cancellationToken)
    {
        int userId = User.GetUserId();

        ProfileResponse result =
            await _profileService.SaveProfileAsync(
                userId,
                request,
                cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves the authenticated user's profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(ProfileRequest),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken)
    {
        int userId = User.GetUserId();

        ProfileRequest? profile =
            await _profileService.GetProfileAsync(
                userId,
                cancellationToken);

        if (profile is null)
        {
            return NotFound(new
            {
                message = "Profile not found."
            });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Uploads a CV for the authenticated user.
    /// </summary>
    [HttpPost("upload-cv")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadCv(
        [FromForm] UploadCvRequest request,
        CancellationToken cancellationToken)
    {
        int userId = User.GetUserId();
        IFormFile file = request.File;

        if (file.Length == 0)
        {
            return BadRequest(new
            {
                message = "No file received."
            });
        }

        string[] allowedExtensions =
        [
            ".pdf",
            ".docx"
        ];

        string extension =
            Path.GetExtension(file.FileName)
                .ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message =
                    "Only PDF and DOCX files are accepted."
            });
        }

        const long maximumFileSize =
            5 * 1024 * 1024;

        if (file.Length > maximumFileSize)
        {
            return BadRequest(new
            {
                message =
                    "File must be smaller than 5 MB."
            });
        }

        await _profileService.SaveCvAsync(
                userId,
                file,
                cancellationToken);

        return Ok(new
        {
            message =
                "CV uploaded and queued for parsing."
        });
    
    }
}

/// <summary>
/// DTO for CV upload requests.
/// </summary>
public sealed class UploadCvRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}