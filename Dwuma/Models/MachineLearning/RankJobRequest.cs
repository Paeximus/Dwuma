namespace Dwuma.Models.MachineLearning;

public sealed class RankJobRequest
{
    public string UserId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;

    public float SkillMatch { get; set; }
    public float LocationMatch { get; set; }
    public float ExperienceMatch { get; set; }
    public float IndustryMatch { get; set; }
    public float SalaryMatch { get; set; }
    public float JobAgeDays { get; set; }
}