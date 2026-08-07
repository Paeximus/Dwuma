namespace Dwuma.Models.Jobs;

public sealed class CreateJobRequest
{
    public string Title { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string JobType { get; set; } = string.Empty;

    public string? Industry { get; set; }

    public string Description { get; set; } = string.Empty;

    public List<string> RequiredSkills { get; set; } = [];

    public decimal? MinimumSalary { get; set; }

    public decimal? MaximumSalary { get; set; }

    public string? SourceUrl { get; set; }

    public DateTime? ExpiresAt { get; set; }
}