using Dwuma.Models;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Dwuma.Controllers
{
    /// <summary>
    /// Manages user profiles and CV documents.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProfileController : ControllerBase
    {
        private readonly ProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        /// <summary>Create or update a user profile.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrUpdate([FromBody] ProfileRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _profileService.SaveProfileAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile.");
                return StatusCode(500, new { message = "Could not save profile. Please try again." });
            }
        }

        /// <summary>Retrieve a profile by its numeric ID.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var profile = await _profileService.GetProfileAsync(id);
            if (profile == null) return NotFound(new { message = "Profile not found." });
            return Ok(profile);
        }

        /// <summary>Upload a CV file (PDF or DOCX) for a user.</summary>
        /// <remarks>
        /// Accepts multipart/form-data. The file is validated and queued for text extraction.
        /// Max file size: 5 MB. Accepted types: .pdf, .docx.
        /// </remarks>
        [HttpPost("upload-cv")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadCv([FromForm] UploadCvRequest request)
        {
            var file = request.File;
            var userId = request.UserId;

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file received." });

            var allowed = new[] { ".pdf", ".docx" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Only PDF and DOCX files are accepted." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File must be smaller than 5MB." });

            try
            {
                await _profileService.SaveCvAsync(userId, file);
                return Ok(new { message = "CV uploaded and queued for parsing." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CV upload failed for user {UserId}", userId);
                return StatusCode(500, new { message = "CV upload failed." });
            }
        }
    }

    /// <summary>
    /// DTO for CV upload requests.
    /// </summary>
    public class UploadCvRequest
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}
