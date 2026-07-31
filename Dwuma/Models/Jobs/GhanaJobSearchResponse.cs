namespace Dwuma.Models.Jobs;

public sealed class GhanaJobSearchResponse
{
    public int ProviderTotalCount { get; set; }

    public int ReturnedCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public string Location { get; set; } = string.Empty;

    public List<GhanaJobResult> Jobs { get; set; } = [];
}
public sealed class GhanaJobResult
{
    public string ExternalId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Salary { get; set; } = string.Empty;

    public string JobType { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string ApplyUrl { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }
}