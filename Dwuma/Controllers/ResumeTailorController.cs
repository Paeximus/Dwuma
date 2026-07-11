using Microsoft.AspNetCore.Mvc;
using Dwuma.Models;
using Dwuma.Services;
using System.ComponentModel.DataAnnotations;

namespace Dwuma.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ResumeTailorController : ControllerBase
    {
        private readonly ResumeTailorService _tailorService;
        private readonly ILogger<ResumeTailorController> _logger;

        public ResumeTailorController(ResumeTailorService tailorService, ILogger<ResumeTailorController> logger)
        {
            _tailorService = tailorService;
            _logger = logger;
        }

        [HttpPost("tailor")]
        [ProducesResponseType(typeof(TailorResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Tailor([FromBody] TailorRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _tailorService.TailorAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tailoring CV for role: {Role}", request.JobTitle);
                return StatusCode(500, new { message = "Tailoring failed. Please try again." });
            }
        }

        [HttpPost("parse-cv")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ParseCv([FromForm] ParseCvRequest request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file received." });

            try
            {
                var text = await _tailorService.ParseCvFileAsync(file);
                return Ok(new { text });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CV parse error.");
                return StatusCode(500, new { message = "Could not parse the file." });
            }
        }

        //[HttpPost("download")]
        //[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.TailoredCv))
        //        return BadRequest(new { message = "No CV content provided." });

        //    try
        //    {
        //        var docxBytes = await _tailorService.GenerateDocxAsync(request);
        //        var fileName = $"CV_{(request.JobTitle ?? "Tailored").Replace(" ", "_")}.docx";
        //        return File(docxBytes,
        //            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        //            fileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "DOCX generation failed.");
        //        return StatusCode(500, new { message = "Could not generate document." });
        //    }
        //}
    }


    public class ParseCvRequest
    {
        [Required]
        public IFormFile File { get; set; }
    }

}
