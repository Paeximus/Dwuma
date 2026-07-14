namespace Dwuma.Models.JobMatching;

public sealed class JobMatchResponse
{
    public int CompatibilityScore { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public List<string> MatchingSkills { get; set; } = [];

    public List<string> MissingSkills { get; set; } = [];

    public List<string> Strengths { get; set; } = [];

    public List<string> Concerns { get; set; } = [];

    public string ApplicationAdvice { get; set; } = string.Empty;
}