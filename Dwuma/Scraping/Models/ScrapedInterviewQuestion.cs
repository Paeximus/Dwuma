namespace Dwuma.Scraping.Models;

public sealed class ScrapedInterviewQuestion
{
    public string Question { get; set; } = string.Empty;

    public string? Role { get; set; }

    public string? Category { get; set; }

    public string Source { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}