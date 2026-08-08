namespace Dwuma.Models.Dashboard;

public sealed class DashboardResponse
{
    public DashboardUserResponse User { get; set; }
        = new();

    public DashboardInterviewResponse? LatestInterview
    { get; set; }

    public List<DashboardSkillResponse> TrendingSkills
    { get; set; } = [];

    public List<DashboardJobResponse> RecommendedJobs
    { get; set; } = [];

    public int UnreadNotifications { get; set; }
}