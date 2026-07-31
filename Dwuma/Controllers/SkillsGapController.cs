using Microsoft.AspNetCore.Mvc;
using Dwuma.Models;
using Dwuma.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;

namespace Dwuma.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ai-policy")]
    [Authorize]
    [Produces("application/json")]
    public class SkillsGapController : ControllerBase
    {
        private readonly SkillsGapService _skillsGapService;
        private readonly ILogger<SkillsGapController> _logger;

        public SkillsGapController(SkillsGapService skillsGapService, ILogger<SkillsGapController> logger)
        {
            _skillsGapService = skillsGapService;
            _logger = logger;
        }

        [HttpPost("analyse")]
        [ProducesResponseType(typeof(SkillsGapResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Analyse([FromBody] SkillsGapRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.Skills == null || request.Skills.Count == 0)
                return BadRequest(new { message = "At least one skill is required." });

            if (string.IsNullOrWhiteSpace(request.JobTitle))
                return BadRequest(new { message = "Job title is required." });

            try
            {
                _logger.LogInformation("Skills gap analysis requested for role: {Role}", request.JobTitle);
                var result = await _skillsGapService.AnalyseAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during skills gap analysis.");
                return StatusCode(500, new { message = "Analysis failed. Please try again." });
            }
        }
    }
}
