using CareerAgent.Training;
using Dwuma.ML;
using Dwuma.Models.MachineLearning;
using Dwuma.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/ml")]
public sealed class MachineLearningController : ControllerBase
{
    private readonly JobRankingService _jobRankingService;

    public MachineLearningController(
        JobRankingService jobRankingService)
    {
        _jobRankingService = jobRankingService;
    }

    [HttpPost("rank-job")]
    public IActionResult RankJob([FromBody] RankJobRequest request)
    {
        var input = new JobInteractionTrainingRow
        {
            UserId = request.UserId,
            JobId = request.JobId,
            SkillMatch = request.SkillMatch,
            LocationMatch = request.LocationMatch,
            ExperienceMatch = request.ExperienceMatch,
            IndustryMatch = request.IndustryMatch,
            SalaryMatch = request.SalaryMatch,
            JobAgeDays = request.JobAgeDays,
            Clicked = 0,
            Saved = 0,
            Applied = 0,
            Rating = 0
        };

        float score = _jobRankingService.Predict(input);

        return Ok(new
        {
            compatibilityScore = score,
            compatibilityPercentage = Math.Round(score * 100, 2)
        });
    }
}