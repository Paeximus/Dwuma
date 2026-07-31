using System.Net.Http.Json;
using System.Text.Json;
using Dwuma.Models.Jobs;

namespace Dwuma.Services;

public sealed class JoobleJobService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JoobleJobService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public JoobleJobService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<JoobleJobService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GhanaJobSearchResponse> SearchAsync(
        GhanaJobSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        string apiKey =
            _configuration["Jooble:ApiKey"]
            ?? throw new InvalidOperationException(
                "Jooble API key is not configured.");

        string baseUrl =
            _configuration["Jooble:BaseUrl"]
            ?? "https://jooble.org/api/";

        string location =
            NormalizeGhanaLocation(request.Location);

        var joobleRequest =
            new JoobleSearchRequest
            {
                Keywords = request.Keywords.Trim(),
                Location = location,
                Page = request.Page.ToString(),
                ResultOnPage = request.PageSize.ToString(),
                CompanySearch =
                    request.CompanySearch
                        ? "true"
                        : "false"
            };

        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                $"{baseUrl.TrimEnd('/')}/{apiKey}",
                joobleRequest,
                JsonOptions,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            _logger.LogError(
                "Jooble request failed with status {StatusCode}. Response: {Response}",
                response.StatusCode,
                error);

            throw new HttpRequestException(
                "The Ghana jobs provider could not complete the request.");
        }

        JoobleSearchResponse? joobleResponse =
            await response.Content.ReadFromJsonAsync<JoobleSearchResponse>(
                JsonOptions,
                cancellationToken);

        if (joobleResponse is null)
        {
            throw new InvalidOperationException(
                "The Ghana jobs provider returned an empty response.");
        }

        List<GhanaJobResult> jobs =
            joobleResponse.Jobs
                .Where(IsGhanaJob)
                .Select(MapJob)
                .Where(job =>
                    !string.IsNullOrWhiteSpace(job.Title) &&
                    !string.IsNullOrWhiteSpace(job.ApplyUrl))
                .GroupBy(job =>
                    $"{job.Title.Trim().ToLowerInvariant()}|" +
                    $"{job.Company.Trim().ToLowerInvariant()}|" +
                    $"{job.Location.Trim().ToLowerInvariant()}")
                .Select(group => group.First())
                .ToList();

        return new GhanaJobSearchResponse
        {
            ProviderTotalCount = joobleResponse.TotalCount,
            ReturnedCount = jobs.Count,
            Page = request.Page,
            PageSize = request.PageSize,
            Location = location,
            Jobs = jobs
        };
    }

    private static GhanaJobResult MapJob(
        JoobleJob source)
    {
        return new GhanaJobResult
        {
            ExternalId =
                source.Id?.ToString()
                ?? string.Empty,

            Title =
                CleanText(source.Title),

            Company =
                CleanText(source.Company),

            Location =
                CleanText(source.Location),

            Description =
                CleanText(source.Snippet),

            Salary =
                CleanText(source.Salary),

            JobType =
                CleanText(source.Type),

            Source =
                CleanText(source.Source),

            ApplyUrl =
                source.Link?.Trim()
                ?? string.Empty,

            UpdatedAt =
                source.Updated
        };
    }

    private static bool IsGhanaJob(
    JoobleJob job)
    {
        string location =
            job.Location?.Trim()
            ?? string.Empty;

        string combined =
            $"{job.Location} {job.Snippet}"
                .ToLowerInvariant();

        string[] ghanaLocations =
        [
            "ghana",
        "accra",
        "greater accra",
        "tema",
        "kumasi",
        "ashanti",
        "takoradi",
        "sekondi",
        "western region",
        "tamale",
        "northern region",
        "cape coast",
        "central region",
        "koforidua",
        "eastern region",
        "sunyani",
        "bono region",
        "ho",
        "volta region",
        "wa",
        "upper west",
        "bolgatanga",
        "upper east",
        "techiman",
        "obuasi"
        ];

        bool isGhanaLocation =
            ghanaLocations.Any(
                ghanaLocation =>
                    combined.Contains(
                        ghanaLocation));

        bool isRemoteGhana =
            location.Contains(
                "remote",
                StringComparison.OrdinalIgnoreCase) &&
            combined.Contains("ghana");

        return isGhanaLocation || isRemoteGhana;
    }

    private static string NormalizeGhanaLocation(
    string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "Ghana";
        }

        string trimmed = location.Trim();

        string[] allowedLocations =
        [
            "ghana",
        "accra",
        "tema",
        "kumasi",
        "takoradi",
        "sekondi",
        "tamale",
        "cape coast",
        "koforidua",
        "sunyani",
        "ho",
        "wa",
        "bolgatanga",
        "techiman",
        "obuasi"
        ];

        bool isAllowed =
            allowedLocations.Any(
                allowed =>
                    trimmed.Contains(
                        allowed,
                        StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            throw new ArgumentException(
                "Location must be within Ghana.");
        }

        if (trimmed.Equals(
                "Ghana",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Ghana";
        }

        if (trimmed.Contains(
                "Ghana",
                StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return $"{trimmed}, Ghana";
    }

    private static string CleanText(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("<b>", string.Empty)
            .Replace("</b>", string.Empty)
            .Replace("&nbsp;", " ")
            .Trim();
    }



}