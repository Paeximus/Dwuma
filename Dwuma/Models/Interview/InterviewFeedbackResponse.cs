namespace Dwuma.Models.Interview;

public sealed class InterviewFeedbackResponse
{
    public int Score { get; set; }

    public string OverallAssessment { get; set; } = string.Empty;

    public List<string> Strengths { get; set; } = [];

    public List<string> Improvements { get; set; } = [];

    public string ImprovedAnswer { get; set; } = string.Empty;

    public string DeliveryTip { get; set; } = string.Empty;
}