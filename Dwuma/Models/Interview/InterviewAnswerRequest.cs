namespace Dwuma.Models.Interview;

public sealed class InterviewAnswerRequest
{
    public int SessionId { get; set; }

    public int QuestionId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public string CandidateAnswer { get; set; } = string.Empty;
}
