using System.Net.Http.Json;
using System.Text.Json;
using Dwuma.Models.Jobs;

namespace Dwuma.Services;

public sealed class JobSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobSearchService> _logger;
    private readonly string _apiKey;

    public JobSearchService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<JobSearchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey =
            configuration["Jooble:ApiKey"]
            ?? throw new InvalidOperationException(
                "Jooble API key is not configured.");
    }

    public async Task<JobSearchResponse> SearchAsync(
        string? query,
        string? location,
        int page = 1,
        bool? remoteOnly = null,
        bool englishOnly = true,
        CancellationToken cancellationToken = default)
    {
        string searchQuery =
            string.IsNullOrWhiteSpace(query)
                ? "jobs"
                : query.Trim();

        string searchLocation =
            string.IsNullOrWhiteSpace(location)
                ? "Ghana"
                : location.Trim();

        page = Math.Max(page, 1);

        var requestBody = new
        {
            keywords = searchQuery,
            location = searchLocation,
            page = page.ToString(),
            ResultOnPage = "30",
            radius = "100",
            companysearch = "false"
        };

        string requestUrl =
            $"https://jooble.org/api/{_apiKey}";

        _logger.LogInformation(
            "Searching Jooble for {Query} in {Location}, page {Page}.",
            searchQuery,
            searchLocation,
            page);

        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                requestUrl,
                requestBody,
                cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Jooble returned status {StatusCode}. Response: {ResponseBody}",
                response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Jooble returned HTTP {(int)response.StatusCode}.");
        }

        JoobleResponse? providerResponse =
            JsonSerializer.Deserialize<JoobleResponse>(
                responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        List<ExternalJobListing> jobs =
            providerResponse?.Jobs?
                .Select(MapJob)
                .ToList()
            ?? [];

        if (remoteOnly == true)
        {
            jobs = jobs
                .Where(job =>
                    job.Remote ||
                    ContainsIgnoreCase(
                        job.Location,
                        "remote") ||
                    job.JobTypes.Any(type =>
                        ContainsIgnoreCase(
                            type,
                            "remote")))
                .ToList();
        }

        jobs = jobs
            .GroupBy(job =>
                !string.IsNullOrWhiteSpace(job.Slug)
                    ? job.Slug
                    : job.Url)
            .Select(group => group.First())
            .OrderByDescending(job => job.CreatedAt)
            .ToList();

        return new JobSearchResponse
        {
            Query = searchQuery,
            Page = page,
            Count = jobs.Count,
            Jobs = jobs
        };
    }

    private static ExternalJobListing MapJob(
        JoobleJob source)
    {
        return new ExternalJobListing
        {
            Slug =
                !string.IsNullOrWhiteSpace(source.Id)
                    ? source.Id
                    : source.Link,

            Title = source.Title ?? string.Empty,

            CompanyName =
                source.Company ?? string.Empty,

            Location =
                source.Location ?? string.Empty,

            Description =
                source.Snippet ?? string.Empty,

            Url =
                source.Link ?? string.Empty,

            Remote =
                ContainsIgnoreCase(
                    source.Location,
                    "remote") ||
                ContainsIgnoreCase(
                    source.Type,
                    "remote"),

            Tags =
                string.IsNullOrWhiteSpace(source.Source)
                    ? []
                    : [source.Source],

            JobTypes =
                string.IsNullOrWhiteSpace(source.Type)
                    ? []
                    : [source.Type],

            CreatedAt = ParseDate(source.Updated)
        };
    }

    private static DateTime? ParseDate(
        string? value)
    {
        if (DateTime.TryParse(
            value,
            out DateTime parsedDate))
        {
            return parsedDate.ToUniversalTime();
        }

        return null;
    }

    private static bool ContainsIgnoreCase(
        string? value,
        string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(
                   searchText,
                   StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class JoobleResponse
{
    public int TotalCount { get; set; }

    public List<JoobleJob> Jobs { get; set; } = [];
}

public sealed class JoobleJob
{
    public string? Id { get; set; }

    public string? Title { get; set; }

    public string? Location { get; set; }

    public string? Company { get; set; }

    public string? Snippet { get; set; }

    public string? Salary { get; set; }

    public string? Source { get; set; }

    public string? Type { get; set; }

    public string? Link { get; set; }

    public string? Updated { get; set; }
}