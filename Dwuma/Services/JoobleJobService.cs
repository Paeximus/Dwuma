using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        ArgumentNullException.ThrowIfNull(request);

        string apiKey =
            _configuration["Jooble:ApiKey"]
            ?? throw new InvalidOperationException(
                "Jooble API key is not configured.");

        string baseUrl =
            _configuration["Jooble:BaseUrl"]
            ?? "https://jooble.org/api/";

        // Add the safe diagnostic log here
        _logger.LogInformation(
            "Jooble configuration: Base URL {BaseUrl}, key configured {HasKey}, key length {KeyLength}.",
            baseUrl,
            !string.IsNullOrWhiteSpace(apiKey),
            apiKey.Length);

        string requestUrl =
            $"{baseUrl.TrimEnd('/')}/{apiKey.Trim()}";

        string location = NormalizeGhanaLocation(request.Location);
        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);

        var joobleRequest = new JoobleSearchRequest
        {
            Keywords = string.IsNullOrWhiteSpace(request.Keywords)
                ? "jobs"
                : request.Keywords.Trim(),
            Location = location,
            Page = page.ToString(),
            ResultOnPage = pageSize.ToString(),
            CompanySearch = request.CompanySearch ? "true" : "false"
        };

        string requestJson =
            JsonSerializer.Serialize(
                joobleRequest,
                JsonOptions);

        string endpoint = $"{baseUrl.TrimEnd('/')}/{apiKey}";

        _logger.LogInformation(
            "Searching Jooble. Keywords: {Keywords}; Location: {Location}; Page: {Page}; PageSize: {PageSize}",
            joobleRequest.Keywords,
            joobleRequest.Location,
            page,
            pageSize);

        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                requestUrl,
                joobleRequest,
                JsonOptions,
                cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation(
            "Jooble returned status {StatusCode}. Response: {ResponseBody}",
            response.StatusCode,
            responseBody);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Jooble request failed with status {StatusCode}. Response: {Response}",
                response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"The Ghana jobs provider returned HTTP {(int)response.StatusCode}.");
        }

        JoobleSearchResponse? joobleResponse =
            JsonSerializer.Deserialize<JoobleSearchResponse>(
                responseBody,
                JsonOptions);

        if (joobleResponse is null)
        {
            throw new InvalidOperationException(
                "The Ghana jobs provider returned an empty response.");
        }

        _logger.LogInformation(
            "Jooble returned {TotalCount} total jobs and {PageCount} jobs on this page.",
            joobleResponse.TotalCount,
            joobleResponse.Jobs.Count);

        // Do not apply a second strict Ghana filter here. Jooble already searched
        // with the Ghana location, and an extra filter can remove valid entries
        // whose location is shown as Remote, Nationwide, or West Africa.
        List<GhanaJobResult> jobs =
            joobleResponse.Jobs
                .Select(
                    (Models.Jobs.JoobleJob job) =>
                        MapJob(job))
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
            Page = page,
            PageSize = pageSize,
            Location = location,
            Jobs = jobs
        };
    }

    private static GhanaJobResult MapJob(
    Dwuma.Models.Jobs.JoobleJob source)
    {
        return new GhanaJobResult
        {
            ExternalId = source.Id?.Trim() ?? string.Empty,
            Title = CleanText(source.Title),
            Company = CleanText(source.Company),
            Location = CleanText(source.Location),
            Description = CleanText(source.Snippet),
            Salary = CleanText(source.Salary),
            JobType = CleanText(source.Type),
            Source = CleanText(source.Source),
            ApplyUrl = source.Link?.Trim() ?? string.Empty,
            UpdatedAt = source.Updated
        };
    }


    private static string NormalizeGhanaLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "Ghana";
        }

        string trimmed = location.Trim();

        string[] allowedLocations =
        [
            "ghana", "accra", "tema", "kumasi", "takoradi", "sekondi",
            "tamale", "cape coast", "koforidua", "sunyani", "ho", "wa",
            "bolgatanga", "techiman", "obuasi"
        ];

        bool isAllowed = allowedLocations.Any(allowed =>
            trimmed.Contains(allowed, StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            throw new ArgumentException("Location must be within Ghana.");
        }

        if (trimmed.Equals("Ghana", StringComparison.OrdinalIgnoreCase))
        {
            return "Ghana";
        }

        if (trimmed.Contains("Ghana", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return $"{trimmed}, Ghana";
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string withoutTags = Regex.Replace(value, "<.*?>", " ");

        return WebUtility
            .HtmlDecode(withoutTags)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }
}
