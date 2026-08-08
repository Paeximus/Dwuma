using Dwuma.Models;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Dwuma.Controllers
{
    [ApiController]
    [Authorize]
    [EnableRateLimiting("ai-policy")]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ResumeTailorController : ControllerBase
    {
        private readonly ResumeTailorService _tailorService;
        private readonly ILogger<ResumeTailorController> _logger;
        private readonly NotificationService _notificationService;

        public ResumeTailorController(
            ResumeTailorService tailorService,
            ILogger<ResumeTailorController> logger,
            NotificationService notificationService)
        {
            _tailorService = tailorService;
            _logger = logger;
            _notificationService = notificationService;
        }

        // ==========================================
        // UPLOAD + PARSE + TAILOR IN ONE ENDPOINT
        // ==========================================

        [HttpPost("tailor")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(
            typeof(TailorResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Tailor(
            [FromForm] TailorCvRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.File == null ||
                request.File.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please upload your CV."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    request.JobTitle))
            {
                return BadRequest(new
                {
                    message = "Job title is required."
                });
            }

            try
            {
                // 1. Parse uploaded CV
                string parsedText =
                    await _tailorService
                        .ParseCvFileAsync(
                            request.File);

                if (string.IsNullOrWhiteSpace(
                        parsedText))
                {
                    return BadRequest(new
                    {
                        message =
                            "No readable text could be extracted from the CV."
                    });
                }

                // 2. Clean parsed CV text
                string cleanedCv =
                    _tailorService
                        .CleanParsedCvText(
                            parsedText);

                // 3. Build the existing tailoring request
                var tailorRequest =
                    new TailorRequest
                    {
                        CvText = cleanedCv,
                        JobTitle =
                            request.JobTitle.Trim(),

                        JobDescription =
                            request.JobDescription?
                                .Trim(),

                        CompanyName =
                            request.CompanyName?
                                .Trim()
                    };

                // 4. Tailor CV
                TailorResponse result =
                    await _tailorService
                        .TailorAsync(
                            tailorRequest);

                // 5. Fix spacing/formatting
                result.TailoredCv =
                    _tailorService
                        .NormalizeTailoredCvText(
                            result.TailoredCv);

                // 6. Create notification
                int userId = GetUserId();

                await _notificationService.CreatePersonalizedAsync(
                     userId,
                     "CV",
                     $"your CV for {request.JobTitle} has been tailored and is ready to download.",
                     cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error parsing and tailoring CV for role {Role}.",
                    request.JobTitle);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Could not process your CV. Please try again."
                    });
            }
        }

        // ==========================================
        // DOWNLOAD FORMATTED DOCX
        // ==========================================

        [HttpPost("download")]
        [ProducesResponseType(
            typeof(FileContentResult),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Download(
            [FromBody] DownloadRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                    request.TailoredCv))
            {
                return BadRequest(new
                {
                    message =
                        "No CV content provided."
                });
            }

            try
            {
                request.TailoredCv =
                    _tailorService
                        .NormalizeTailoredCvText(
                            request.TailoredCv);

                byte[] docxBytes =
                    await _tailorService
                        .GenerateDocxAsync(
                            request);

                string safeJobTitle =
                    string.IsNullOrWhiteSpace(
                        request.JobTitle)
                        ? "Tailored"
                        : request.JobTitle.Trim();

                foreach (char invalidChar
                         in Path.GetInvalidFileNameChars())
                {
                    safeJobTitle =
                        safeJobTitle.Replace(
                            invalidChar,
                            '_');
                }

                safeJobTitle =
                    safeJobTitle.Replace(
                        " ",
                        "_");

                string fileName =
                    $"DWUMA_CV_{safeJobTitle}.docx";

                return File(
                    docxBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "DOCX generation failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Could not generate document."
                    });
            }
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
                    "Unable to identify the current user.");
            }

            return userId;
        }
    }

    public sealed class TailorCvRequest
    {
        [Required]
        public IFormFile File { get; set; }
            = null!;

        [Required]
        public string JobTitle { get; set; }
            = string.Empty;

        public string? JobDescription { get; set; }

        public string? CompanyName { get; set; }
    }
}