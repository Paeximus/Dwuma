using System.Text.Json.Serialization;

namespace Dwuma.Models.Jobs;

public sealed class SerpApiResponse
{
    [JsonPropertyName("jobs_results")]
    public List<SerpApiJob> JobsResults { get; set; } = [];

    [JsonPropertyName("serpapi_pagination")]
    public SerpApiPagination? SerpApiPagination { get; set; }
}

public sealed class SerpApiJob
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("job_id")]
    public string? JobId { get; set; }

    [JsonPropertyName("via")]
    public string? Via { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("detected_extensions")]
    public SerpApiDetectedExtensions? DetectedExtensions { get; set; }

    [JsonPropertyName("apply_options")]
    public List<SerpApiApplyOption> ApplyOptions { get; set; } = [];
}

public sealed class SerpApiDetectedExtensions
{
    [JsonPropertyName("posted_at")]
    public string? PostedAt { get; set; }

    [JsonPropertyName("schedule_type")]
    public string? ScheduleType { get; set; }

    [JsonPropertyName("work_from_home")]
    public bool? WorkFromHome { get; set; }
}

public sealed class SerpApiApplyOption
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }
}

public sealed class SerpApiPagination
{
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }
}