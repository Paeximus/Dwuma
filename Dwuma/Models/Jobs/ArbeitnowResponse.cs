using System.Text.Json.Serialization;
using Dwuma.Converters;

namespace Dwuma.Models.Jobs;

public sealed class ArbeitnowResponse
{
    [JsonPropertyName("data")]
    public List<ArbeitnowJob> Data { get; set; } = [];

    [JsonPropertyName("links")]
    public ArbeitnowLinks? Links { get; set; }

    [JsonPropertyName("meta")]
    public ArbeitnowMeta? Meta { get; set; }
}

public sealed class ArbeitnowJob
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("remote")]
    public bool Remote { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("job_types")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public List<string> JobTypes { get; set; } = new();

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}

public sealed class ArbeitnowLinks
{
    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

public sealed class ArbeitnowMeta
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }
}