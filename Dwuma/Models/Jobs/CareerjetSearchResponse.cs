using System.Text.Json.Serialization;

namespace Dwuma.Models.Jobs;

public sealed class CareerjetSearchResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("hits")]
    public int Hits { get; set; }

    [JsonPropertyName("jobs")]
    public List<CareerjetJob> Jobs { get; set; } = [];

    [JsonPropertyName("pages")]
    public int Pages { get; set; }
}