using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Dwuma.Models.Jobs;

namespace Dwuma.Services;

public sealed class JobSearchService
{
    private const string BaseUrl =
        "https://www.arbeitnow.com/api/job-board-api";

    private readonly HttpClient _httpClient;
    private readonly ILogger<JobSearchService> _logger;

    public JobSearchService(
        HttpClient httpClient,
        ILogger<JobSearchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<JobSearchResponse> SearchAsync(
        string? query,
        int page = 1,
        bool? remoteOnly = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);

        string requestUrl =
            $"{BaseUrl}?page={page}";

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                requestUrl,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            _logger.LogError(
                "Arbeitnow failed with status {StatusCode}. Body: {Body}",
                response.StatusCode,
                errorBody);

            throw new InvalidOperationException(
                $"Job provider returned status {(int)response.StatusCode}.");
        }

        ArbeitnowResponse? providerResponse =
            await response.Content
                .ReadFromJsonAsync<ArbeitnowResponse>(
                    cancellationToken: cancellationToken);

        List<ExternalJobListing> jobs =
            providerResponse?.Data
                .Select(MapJob)
                .ToList()
            ?? [];

        if (!string.IsNullOrWhiteSpace(query))
        {
            string searchText =
                query.Trim();

            jobs = jobs
                .Where(job =>
                    ContainsIgnoreCase(
                        job.Title,
                        searchText) ||
                    ContainsIgnoreCase(
                        job.CompanyName,
                        searchText) ||
                    ContainsIgnoreCase(
                        job.Description,
                        searchText) ||
                    job.Tags.Any(tag =>
                        ContainsIgnoreCase(
                            tag,
                            searchText)))
                .ToList();
        }

        if (remoteOnly == true)
        {
            jobs = jobs
                .Where(job => job.Remote)
                .ToList();
        }

        return new JobSearchResponse
        {
            Query = query?.Trim() ?? string.Empty,
            Page = page,
            Count = jobs.Count,
            Jobs = jobs
        };
    }

    private static ExternalJobListing MapJob(
        ArbeitnowJob source)
    {
        return new ExternalJobListing
        {
            Slug = source.Slug,
            Title = source.Title,
            CompanyName = source.CompanyName,
            Location = source.Location,
            Description = StripHtml(
                source.Description),
            Url = source.Url,
            Remote = source.Remote,
            Tags = source.Tags ?? [],
            JobTypes = source.JobTypes ?? [],
            CreatedAt =
                source.CreatedAt > 0
                    ? DateTimeOffset
                        .FromUnixTimeSeconds(
                            source.CreatedAt)
                        .UtcDateTime
                    : null
        };
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

    private static string StripHtml(
        string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        string withoutTags =
            Regex.Replace(
                html,
                "<.*?>",
                " ");

        return System.Net.WebUtility
            .HtmlDecode(withoutTags)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }
}