namespace Dwuma.Models.Jobs;

public sealed class JobSearchResponse
{
    public string Query { get; set; } = string.Empty;

    public int Page { get; set; }

    public int Count { get; set; }

    public List<ExternalJobListing> Jobs { get; set; } = [];
}