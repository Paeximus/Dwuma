namespace Dwuma.Models.Interview;

public sealed class InterviewQuestionRequest
{
    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public string CandidateSkills { get; set; } = string.Empty;

    public string CandidateExperience { get; set; } = string.Empty;

    public int NumberOfQuestions { get; set; } = 5;
}