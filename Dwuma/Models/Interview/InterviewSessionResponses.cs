namespace Dwuma.Models.Interview;

public sealed class InterviewCompletionResponse
{
    public int SessionId { get; set; }
    public bool Completed { get; set; }
    public int Score { get; set; }
    public int QuestionsAnswered { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime CompletedAt { get; set; }
}

public sealed class LatestInterviewResponse
{
    public int SessionId { get; set; }
    public bool Completed { get; set; }
    public int Score { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public int QuestionsAnswered { get; set; }
    public DateTime CompletedAt { get; set; }
}
