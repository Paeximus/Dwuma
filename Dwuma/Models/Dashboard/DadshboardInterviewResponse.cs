namespace Dwuma.Models.Dashboard;

public sealed class DashboardInterviewResponse
{
    public bool Completed { get; set; }

    public int Score { get; set; }

    public string Message { get; set; }
        = string.Empty;

    public string? Feedback { get; set; }

    public DateTime? CompletedAt { get; set; }
}