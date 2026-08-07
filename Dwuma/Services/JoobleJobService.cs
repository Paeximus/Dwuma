using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dwuma.Models.Jobs;
using Dwuma.Models.Data.DwumaContext;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services;

public sealed class JoobleJobService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JoobleJobService> _logger;
    private readonly DwumaContext _db;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public JoobleJobService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<JoobleJobService> logger,
        DwumaContext db)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _db = db;
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

        List<Dwuma.Models.Jobs.JoobleJob> providerJobs =
            joobleResponse.Jobs ?? [];

        await CacheJobsAsync(
            providerJobs,
            cancellationToken);

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

    

    private static DateTime? ParseUpdatedDate(
    string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(
                value,
                out DateTime parsedDate))
        {
            return parsedDate.ToUniversalTime();
        }

        return null;
    }

    private static JobListing MapToJobListing(
    Dwuma.Models.Jobs.JoobleJob source)
    {
        return new JobListing
        {
            ExternalId =
                source.Id?.Trim(),

            Title =
                CleanText(source.Title),

            Company =
                CleanText(source.Company),

            Location =
                CleanText(source.Location),

            JobType =
                CleanText(source.Type),

            Description =
                CleanText(source.Snippet),

            Salary =
                CleanText(source.Salary),

            Source =
                string.IsNullOrWhiteSpace(source.Source)
                    ? "Jooble"
                    : CleanText(source.Source),

            SourceUrl =
                source.Link?.Trim(),

            PostedAt =source.Updated,

            DiscoveredAt =
                DateTime.UtcNow,

            Status =
                "Active",

            IsRemote =
                IsRemoteJob(
                    source.Title,
                    source.Location,
                    source.Snippet)
        };
    }


    private static bool IsRemoteJob(
    params string?[] values)
    {
        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            (
                value.Contains(
                    "remote",
                    StringComparison.OrdinalIgnoreCase) ||
                value.Contains(
                    "work from home",
                    StringComparison.OrdinalIgnoreCase) ||
                value.Contains(
                    "hybrid",
                    StringComparison.OrdinalIgnoreCase)
            ));
    }

    private async Task CacheJobsAsync(
    IEnumerable<Dwuma.Models.Jobs.JoobleJob> providerJobs,
    CancellationToken cancellationToken)
    {
        foreach (
            Dwuma.Models.Jobs.JoobleJob providerJob
            in providerJobs)
        {
            JobListing incoming =
                MapToJobListing(providerJob);

            if (string.IsNullOrWhiteSpace(
                    incoming.ExternalId) ||
                string.IsNullOrWhiteSpace(
                    incoming.Source))
            {
                continue;
            }

            JobListing? existing =
                await _db.JobListings
                    .FirstOrDefaultAsync(
                        job =>
                            job.ExternalId ==
                                incoming.ExternalId &&
                            job.Source ==
                                incoming.Source,
                        cancellationToken);

            if (existing is null)
            {
                _db.JobListings.Add(incoming);
                continue;
            }

            existing.Title =
                incoming.Title;

            existing.Company =
                incoming.Company;

            existing.Location =
                incoming.Location;

            existing.JobType =
                incoming.JobType;

            existing.Description =
                incoming.Description;

            existing.Salary =
                incoming.Salary;

            existing.SourceUrl =
                incoming.SourceUrl;

            existing.PostedAt =
                incoming.PostedAt;

            existing.IsRemote =
                incoming.IsRemote;

            existing.Status =
                "Active";

            existing.DiscoveredAt =
                DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<GhanaJobSearchResponse> GetCachedJobsAsync(
    GhanaJobSearchRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);

        string location =
            NormalizeGhanaLocation(request.Location);

        IQueryable<JobListing> query =
            _db.JobListings
                .AsNoTracking()
                .Where(job =>
                    job.Status == "Active" &&
                    (
                        !job.ExpiresAt.HasValue ||
                        job.ExpiresAt > DateTime.UtcNow
                    ));

        if (!string.IsNullOrWhiteSpace(
                request.Keywords))
        {
            string keywords =
                request.Keywords.Trim();

            query = query.Where(job =>
                job.Title.Contains(keywords) ||
                (
                    job.Company != null &&
                    job.Company.Contains(keywords)
                ) ||
                (
                    job.Description != null &&
                    job.Description.Contains(keywords)
                ));
        }

        if (!location.Equals(
                "Ghana",
                StringComparison.OrdinalIgnoreCase))
        {
            string city =
                location.Replace(
                    ", Ghana",
                    "",
                    StringComparison.OrdinalIgnoreCase)
                .Trim();

            query = query.Where(job =>
                job.Location != null &&
                job.Location.Contains(city));
        }

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        List<JobListing> cachedJobs =
            await query
                .OrderByDescending(job =>
                    job.PostedAt ??
                    job.DiscoveredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(
                    cancellationToken);

        List<GhanaJobResult> jobs =
            cachedJobs
                .Select(job => new GhanaJobResult
                {
                    ExternalId =
                        job.ExternalId ??
                        job.Id.ToString(),

                    Title =
                        job.Title,

                    Company =
                        job.Company ??
                        string.Empty,

                    Location =
                        job.Location ??
                        string.Empty,

                    Description =
                        job.Description ??
                        string.Empty,

                    Salary =
                        job.Salary ??
                        string.Empty,

                    JobType =
                        job.JobType ??
                        string.Empty,

                    Source =
                        job.Source ??
                        string.Empty,

                    ApplyUrl =
                        job.SourceUrl ??
                        string.Empty,

                    UpdatedAt =
                        job.PostedAt
                })
                .ToList();

        return new GhanaJobSearchResponse
        {
            ProviderTotalCount =
                totalCount,

            ReturnedCount =
                jobs.Count,

            Page =
                page,

            PageSize =
                pageSize,

            Location =
                location,

            Jobs =
                jobs
        };
    }
}
