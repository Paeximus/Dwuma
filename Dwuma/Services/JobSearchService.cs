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
     string? location,
     int page = 1,
     bool? remoteOnly = null,
     bool englishOnly = false,
     CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);

        const int pagesToSearch = 5;
        const int maximumResults = 30;

        var jobs = new List<ExternalJobListing>();

        for (
            int currentPage = page;
            currentPage < page + pagesToSearch;
            currentPage++)
        {
            string requestUrl =
                $"{BaseUrl}?page={currentPage}";

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        requestUrl,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody =
                        await response.Content.ReadAsStringAsync(
                            cancellationToken);

                    _logger.LogWarning(
                        "Arbeitnow page {Page} returned status {StatusCode}. Body: {Body}",
                        currentPage,
                        response.StatusCode,
                        errorBody);

                    continue;
                }

                ArbeitnowResponse? providerResponse =
                    await response.Content
                        .ReadFromJsonAsync<ArbeitnowResponse>(
                            cancellationToken:
                                cancellationToken);

                if (providerResponse?.Data is null ||
                    providerResponse.Data.Count == 0)
                {
                    continue;
                }

                jobs.AddRange(
                    providerResponse.Data.Select(MapJob));
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "The request for Arbeitnow page {Page} timed out.",
                    currentPage);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "The request for Arbeitnow page {Page} failed.",
                    currentPage);
            }
        }

        if (jobs.Count == 0)
        {
            throw new InvalidOperationException(
                "The job provider returned no available jobs.");
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            string[] searchTerms =
                query.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            jobs = jobs
                .Where(job =>
                    searchTerms.Any(term =>
                        ContainsIgnoreCase(
                            job.Title,
                            term) ||
                        ContainsIgnoreCase(
                            job.CompanyName,
                            term) ||
                        ContainsIgnoreCase(
                            job.Description,
                            term) ||
                        ContainsIgnoreCase(
                            job.Location,
                            term) ||
                        job.Tags.Any(tag =>
                            ContainsIgnoreCase(
                                tag,
                                term)) ||
                        job.JobTypes.Any(jobType =>
                            ContainsIgnoreCase(
                                jobType,
                                term))))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            string locationSearch =
                location.Trim();

            jobs = jobs
                .Where(job =>
                    ContainsIgnoreCase(
                        job.Location,
                        locationSearch))
                .ToList();
        }

        if (englishOnly)
        {
            jobs = jobs
                .Where(IsProbablyEnglish)
                .ToList();
        }

        if (remoteOnly == true)
        {
            jobs = jobs
                .Where(job => job.Remote)
                .ToList();
        }

        jobs = jobs
            .GroupBy(job =>
                !string.IsNullOrWhiteSpace(job.Slug)
                    ? job.Slug
                    : job.Url)
            .Select(group => group.First())
            .OrderByDescending(job => job.CreatedAt)
            .Take(maximumResults)
            .ToList();

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

    private static bool IsProbablyEnglish(
    ExternalJobListing job)
    {
        string combinedText =
            $"{job.Title} {job.Description}"
                .ToLowerInvariant();

        combinedText = combinedText
            .Replace(
                "find more english speaking jobs in germany on arbeitnow",
                string.Empty)
            .Replace(
                "find english speaking jobs in germany on arbeitnow",
                string.Empty)
            .Replace(
                "find jobs in germany on arbeitnow",
                string.Empty);

        string[] strongGermanIndicators =
        [
            " aufgaben ",
        " qualifikation ",
        " deine aufgaben ",
        " das bringst du mit ",
        " wir suchen ",
        " wir bieten ",
        " was dich erwartet ",
        " berufserfahrung ",
        " deutschkenntnisse ",
        " bewerbung ",
        " ausbildung ",
        " vollzeit ",
        " teilzeit ",
        " kenntnisse ",
        " verantwortung ",
        " unser team ",
        " wir freuen uns "
        ];

        string[] strongEnglishIndicators =
        [
            " responsibilities ",
        " requirements ",
        " qualifications ",
        " about the role ",
        " about you ",
        " your responsibilities ",
        " what you will do ",
        " what we offer ",
        " we are looking for ",
        " you will ",
        " join our team ",
        " apply now "
        ];

        int germanScore =
            strongGermanIndicators.Count(indicator =>
                combinedText.Contains(indicator));

        int englishScore =
            strongEnglishIndicators.Count(indicator =>
                combinedText.Contains(indicator));

        return englishScore >= 2 &&
               germanScore == 0;
    }
}