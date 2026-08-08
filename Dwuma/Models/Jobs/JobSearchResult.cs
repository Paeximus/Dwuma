using Dwuma.Models.Data.DwumaContext;

namespace Dwuma.Models.Jobs;

public sealed class JobSearchResult
{
    public List<JobListing> Jobs { get; set; } = [];

    public string? NextPageToken { get; set; }

    public bool HasMore =>
        !string.IsNullOrWhiteSpace(
            NextPageToken);

    public bool FromCache { get; set; }
}