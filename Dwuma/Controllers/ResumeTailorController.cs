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
    public class ResumeTailorController : ControllerBase
    {
        private readonly ResumeTailorService _tailorService;
        private readonly ILogger<ResumeTailorController> _logger;
        private readonly NotificationService _notificationService;

        private const string DocxContentType =
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

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
        // UPLOAD + PARSE + TAILOR
        // POST /api/ResumeTailor/tailor
        // ==========================================

        [HttpPost("tailor")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(
            typeof(TailorResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
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

                // 3. Build tailoring request
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

                // 4. Tailor CV with Gemini
                TailorResponse result =
                    await _tailorService
                        .TailorAsync(
                            tailorRequest,
                            cancellationToken);

                // 5. Normalize returned CV text
                result.TailoredCv =
                    _tailorService
                        .NormalizeTailoredCvText(
                            result.TailoredCv);

                // 6. Create notification
                int userId = GetUserId();

                await _notificationService
                    .CreatePersonalizedAsync(
                        userId,
                        "CV",
                        $"Your CV for {request.JobTitle.Trim()} has been tailored and is ready to download.",
                        cancellationToken);

                // Important:
                // This remains JSON because the frontend still
                // needs the tailored CV, ATS score, changelog, etc.
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(
                    StatusCodes.Status408RequestTimeout,
                    new
                    {
                        message =
                            "The CV tailoring request was cancelled."
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
        // DOWNLOAD TAILORED CV AS DOCX
        // POST /api/ResumeTailor/download
        // ==========================================

        [HttpPost("download")]
        [Consumes("application/json")]
        [Produces(DocxContentType)]
        [ProducesResponseType(
            typeof(FileContentResult),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadTailoredCv(
            [FromBody] DownloadRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(
                    request.TailoredCv))
            {
                return BadRequest(new
                {
                    message =
                        "Tailored CV content is required."
                });
            }

            try
            {
                byte[] document =
                    await _tailorService
                        .GenerateDocxAsync(request);

                if (document.Length == 0)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            message =
                                "The generated CV document was empty."
                        });
                }

                string fileName =
                    $"DWUMA_Tailored_CV_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx";

                return File(
                    document,
                    DocxContentType,
                    fileName);
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
                    "Failed to generate tailored CV DOCX.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "The tailored CV document could not be generated."
                    });
            }
        }

        // ==========================================
        // CURRENT USER
        // ==========================================

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

    // ==========================================
    // MULTIPART FORM REQUEST
    // ==========================================

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