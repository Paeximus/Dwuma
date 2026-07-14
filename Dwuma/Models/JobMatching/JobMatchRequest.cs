namespace Dwuma.Models.JobMatching;

public sealed class JobMatchRequest
{
    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public string JobLocation { get; set; } = string.Empty;

    public List<string> CandidateSkills { get; set; } = [];

    public string CandidateEducation { get; set; } = string.Empty;

    public string CandidateExperience { get; set; } = string.Empty;

    public string PreferredLocation { get; set; } = string.Empty;

    public string CareerGoals { get; set; } = string.Empty;
}