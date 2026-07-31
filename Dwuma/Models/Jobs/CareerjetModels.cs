using System.Text.Json.Serialization;

namespace Dwuma.Models.Jobs;

public sealed class CareerjetSearchResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("hits")]
    public int Hits { get; set; }

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("jobs")]
    public List<CareerjetJob> Jobs { get; set; } = [];
}

public sealed class CareerjetJob
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("company")]
    public string Company { get; set; } = string.Empty;

    [JsonPropertyName("locations")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("salary")]
    public string Salary { get; set; } = string.Empty;

    [JsonPropertyName("site")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string ApplyUrl { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;
}