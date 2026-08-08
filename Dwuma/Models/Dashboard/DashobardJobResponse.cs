namespace Dwuma.Models.Dashboard;

public sealed class DashboardJobResponse
{
    public int Id { get; set; }

    public string Title { get; set; }
        = string.Empty;

    public string? Company { get; set; }

    public string? Location { get; set; }

    public string? JobType { get; set; }

    public bool IsRemote { get; set; }

    public string? ApplyUrl { get; set; }

    public DateTime? PostedAt { get; set; }
}