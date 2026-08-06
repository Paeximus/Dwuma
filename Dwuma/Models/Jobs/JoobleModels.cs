using System.Text.Json.Serialization;

namespace Dwuma.Models.Jobs;


public sealed class JoobleSearchRequest
{
    [JsonPropertyName("keywords")]
    public string Keywords { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = "Ghana";

    [JsonPropertyName("page")]
    public string Page { get; set; } = "1";

    [JsonPropertyName("ResultOnPage")]
    public string ResultOnPage { get; set; } = "20";

    [JsonPropertyName("companysearch")]
    public string CompanySearch { get; set; } = "false";
}

public sealed class JoobleSearchResponse
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("jobs")]
    public List<JoobleJob> Jobs { get; set; } = [];
}

public sealed class JoobleJob
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }

    [JsonPropertyName("salary")]
    public string? Salary { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }
}
