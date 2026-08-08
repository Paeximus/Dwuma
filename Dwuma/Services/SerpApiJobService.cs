using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Jobs;
using System.Text.Json;
using Dwuma.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Dwuma.Services;

public sealed class SerpApiJobService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SerpApiJobService> _logger;
    private readonly DwumaContext _dbContext;

    public SerpApiJobService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<SerpApiJobService> logger,
    DwumaContext dbContext)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<SerpApiResponse> SearchJobsAsync(
        string query,
        string location = "Accra, Ghana",
        string? nextPageToken = null,
        CancellationToken cancellationToken = default)
    {
        string apiKey =
            _configuration["SerpApi:ApiKey"]
            ?? throw new InvalidOperationException(
                "SerpApi API key is not configured.");

        if (string.IsNullOrWhiteSpace(query))
        {
            query = "jobs";
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            location = "Accra, Ghana";
        }

        string url =
            "https://serpapi.com/search.json" +
            "?engine=google_jobs" +
            $"&q={Uri.EscapeDataString(query)}" +
            $"&location={Uri.EscapeDataString(location)}" +
            "&gl=gh" +
            "&hl=en" +
            $"&api_key={Uri.EscapeDataString(apiKey)}";

        if (!string.IsNullOrWhiteSpace(nextPageToken))
        {
            url +=
                $"&next_page_token={Uri.EscapeDataString(nextPageToken)}";
        }

        _logger.LogInformation(
            "Searching SerpApi jobs. Query: {Query}, Location: {Location}",
            query,
            location);

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                url,
                cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "SerpApi request failed. Status: {StatusCode}. Response: {Response}",
                response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"SerpApi request failed with HTTP {(int)response.StatusCode}.");
        }

        SerpApiResponse? result =
            JsonSerializer.Deserialize<SerpApiResponse>(
                responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result is null)
        {
            throw new InvalidOperationException(
                "SerpApi returned an empty response.");
        }

        _logger.LogInformation(
            "SerpApi returned {Count} jobs.",
            result.JobsResults.Count);

        return result;
    }

    private static JobListing MapToJobListing(
    SerpApiJob source)
    {
        string? applyUrl =
            source.ApplyOptions
                .FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x.Link))
                ?.Link;

        return new JobListing
        {
            ExternalId =
                CreateExternalId(source),

            Title =
                source.Title?.Trim()
                ?? "Untitled Job",

            Company =
                source.CompanyName?.Trim(),

            Location =
                source.Location?.Trim(),

            Description =
                source.Description?.Trim(),

            JobType =
                source.DetectedExtensions
                    ?.ScheduleType,

            IsRemote =
                source.DetectedExtensions
                    ?.WorkFromHome
                ?? false,

            PostedAt =
                ParsePostedAt(
                    source.DetectedExtensions?
                        .PostedAt),

            Source =
                "SerpApi",

            SourceUrl =
                applyUrl,

            Status =
                "Active",

            DiscoveredAt =
                DateTime.UtcNow,

            
        };
    }

    private static string CreateExternalId(
    SerpApiJob source)
    {
        string value;

        if (!string.IsNullOrWhiteSpace(
                source.JobId))
        {
            value = source.JobId;
        }
        else
        {
            value =
                $"{source.Title}|" +
                $"{source.CompanyName}|" +
                $"{source.Location}";
        }

        byte[] bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes)
            .ToLowerInvariant();
    }

    public async Task<List<JobListing>> SearchMappedJobsAsync(
    string query,
    string location = "Accra, Ghana",
    CancellationToken cancellationToken = default)
    {
        SerpApiResponse response =
            await SearchJobsAsync(
                query,
                location,
                cancellationToken:
                    cancellationToken);

        return response.JobsResults
            .Where(job =>
                !string.IsNullOrWhiteSpace(
                    job.Title))
            .Select(MapToJobListing)
            .ToList();
    }

    public async Task<List<JobListing>> SearchAndCacheJobsAsync(
    string query,
    string location = "Accra, Ghana",
    CancellationToken cancellationToken = default)
    {
        List<JobListing> jobs =
            await SearchMappedJobsAsync(
                query,
                location,
                cancellationToken);

        foreach (JobListing job in jobs)
        {
            if (string.IsNullOrWhiteSpace(
                    job.ExternalId))
            {
                continue;
            }

            JobListing? existingJob =
                await _dbContext.JobListings
                    .FirstOrDefaultAsync(
                        existing =>
                            existing.Source == "SerpApi" &&
                            existing.ExternalId ==
                                job.ExternalId,
                        cancellationToken);

            if (existingJob is null)
            {
                _dbContext.JobListings.Add(job);
            }
            else
            {
                existingJob.Title =
                    job.Title;

                existingJob.Company =
                    job.Company;

                existingJob.Location =
                    job.Location;

                existingJob.Description =
                    job.Description;

                existingJob.JobType =
                    job.JobType;

                existingJob.IsRemote =
                    job.IsRemote;

                existingJob.SourceUrl =
                    job.SourceUrl;

                existingJob.Status =
                    "Active";
            }
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Processed {Count} SerpApi jobs for caching.",
            jobs.Count);

        return jobs;
    }

    public async Task<JobSearchResult> SearchJobsWithPaginationAsync(
    string query,
    string location = "Accra, Ghana",
    string? jobType = null,
    bool? remote = null,
    string? nextPageToken = null,
    CancellationToken cancellationToken = default)
    {
        string normalizedQuery =
            string.IsNullOrWhiteSpace(query)
                ? "jobs"
                : query.Trim();

        string normalizedLocation =
            string.IsNullOrWhiteSpace(location)
                ? "Accra, Ghana"
                : location.Trim();

        await MarkExpiredJobsAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(nextPageToken))
        {
            DateTime freshnessCutoff =
                DateTime.UtcNow.AddHours(-6);

            string[] searchTerms =
                normalizedQuery.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            IQueryable<JobListing> cachedQuery =
                _dbContext.JobListings
                    .AsNoTracking()
                    .Where(job =>
                        job.Source == "SerpApi" &&
                        job.Status == "Active" &&
                        job.DiscoveredAt >= freshnessCutoff);

            if (!string.IsNullOrWhiteSpace(normalizedLocation) && remote != true)
            {
                cachedQuery =
                    cachedQuery.Where(job =>
                        job.Location == null ||
                        job.Location.Contains(
                            normalizedLocation) ||
                        normalizedLocation.Contains(
                            job.Location));
            }

            // Location filter
            if (!string.IsNullOrWhiteSpace(
                    normalizedLocation))
            {
                cachedQuery =
                    cachedQuery.Where(job =>
                        job.Location == null ||
                        job.Location.Contains(
                            normalizedLocation) ||
                        normalizedLocation.Contains(
                            job.Location));
            }

            // Job type filter
            if (!string.IsNullOrWhiteSpace(
                    jobType))
            {
                string normalizedJobType =
                    jobType.Trim();

                cachedQuery =
                    cachedQuery.Where(job =>
                        job.JobType != null &&
                        job.JobType.Contains(
                            normalizedJobType));
            }

            // Remote filter
            if (remote.HasValue)
            {
                cachedQuery =
                    cachedQuery.Where(job =>
                        job.IsRemote ==
                        remote.Value);
            }

            foreach (string term in searchTerms)
            {
                string searchTerm = term;

                cachedQuery =
                    cachedQuery.Where(job =>
                        job.Title.Contains(searchTerm) ||
                        (
                            job.Description != null &&
                            job.Description.Contains(searchTerm)
                        ));
            }

            List<JobListing> cachedJobs =
                await cachedQuery
                    .OrderByDescending(job =>
                        job.DiscoveredAt)
                    .Take(20)
                    .ToListAsync(
                        cancellationToken);

            if (cachedJobs.Count > 0)
            {
                _logger.LogInformation(
                    "Returning {Count} cached jobs for {Query}.",
                    cachedJobs.Count,
                    normalizedQuery);

                return new JobSearchResult
                {
                    Jobs = cachedJobs,
                    NextPageToken = null,
                    FromCache = true
                };
            }
        }

        SerpApiResponse apiResponse =
            await SearchJobsAsync(
                normalizedQuery,
                normalizedLocation,
                nextPageToken,
                cancellationToken);

        List<JobListing> mappedJobs =
            apiResponse.JobsResults
                .Where(job =>
                    !string.IsNullOrWhiteSpace(job.Title))
                .Select(MapToJobListing)
                .ToList();

        mappedJobs =ApplyJobFilters(
                mappedJobs,
                normalizedLocation,
                jobType,
                remote);

        foreach (JobListing job in mappedJobs)
        {
            if (string.IsNullOrWhiteSpace(job.ExternalId))
            {
                continue;
            }

            JobListing? existingJob =
                await _dbContext.JobListings
                    .FirstOrDefaultAsync(
                        existing =>
                            existing.Source == "SerpApi" &&
                            existing.ExternalId == job.ExternalId,
                        cancellationToken);

            if (existingJob == null)
            {
                _dbContext.JobListings.Add(job);
            }
            else
            {
                existingJob.Title = job.Title;
                existingJob.Company = job.Company;
                existingJob.Location = job.Location;
                existingJob.Description = job.Description;
                existingJob.JobType = job.JobType;
                existingJob.IsRemote = job.IsRemote;
                existingJob.SourceUrl = job.SourceUrl;
                existingJob.Status = "Active";
                existingJob.DiscoveredAt = DateTime.UtcNow;
                existingJob.PostedAt = job.PostedAt;
            }
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new JobSearchResult
        {
            Jobs = mappedJobs,

            NextPageToken =
                apiResponse.SerpApiPagination?
                    .NextPageToken,

            FromCache = false
        };
    }

    public async Task<JobListing?> GetJobByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.JobListings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                job => job.Id == id && job.Status == "Active",
                cancellationToken);
    }

    public async Task<int> MarkExpiredJobsAsync(
    CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        List<JobListing> expiredJobs =
            await _dbContext.JobListings
                .Where(job =>
                    job.Status == "Active" &&
                    (
                        (job.ExpiresAt != null &&
                         job.ExpiresAt <= now)
                        ||
                        (
                            job.ExpiresAt == null &&
                            job.DiscoveredAt <= now.AddDays(-30)
                        )
                    ))
                .ToListAsync(
                    cancellationToken);

        if (expiredJobs.Count == 0)
        {
            return 0;
        }

        foreach (JobListing job in expiredJobs)
        {
            job.Status = "Expired";
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Marked {Count} jobs as expired.",
            expiredJobs.Count);

        return expiredJobs.Count;
    }

    private static DateTime? ParsePostedAt(
    string? postedAt)
    {
        if (string.IsNullOrWhiteSpace(postedAt))
        {
            return null;
        }

        string value =
            postedAt.Trim()
                .ToLowerInvariant();

        DateTime now =
            DateTime.UtcNow;

        if (value.Contains("today") ||
            value.Contains("just posted"))
        {
            return now;
        }

        if (value.Contains("yesterday"))
        {
            return now.AddDays(-1);
        }

        var match =
            System.Text.RegularExpressions.Regex.Match(
                value,
                @"(\d+)\+?\s*(minute|minutes|hour|hours|day|days|week|weeks|month|months)");

        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(
                match.Groups[1].Value,
                out int amount))
        {
            return null;
        }

        string unit =
            match.Groups[2].Value;

        return unit switch
        {
            "minute" or "minutes" =>
                now.AddMinutes(-amount),

            "hour" or "hours" =>
                now.AddHours(-amount),

            "day" or "days" =>
                now.AddDays(-amount),

            "week" or "weeks" =>
                now.AddDays(-(amount * 7)),

            "month" or "months" =>
                now.AddMonths(-amount),

            _ => null
        };
    }

    private static List<JobListing> ApplyJobFilters(
    IEnumerable<JobListing> jobs,
    string? location,
    string? jobType,
    bool? remote)
    {
        IEnumerable<JobListing> filtered =
            jobs;

        if (!string.IsNullOrWhiteSpace(location) && remote != true)
        {
            string normalizedLocation =
                location.Trim();

            filtered =
                filtered.Where(job =>
                    string.IsNullOrWhiteSpace(
                        job.Location) ||
                    job.Location.Contains(
                        normalizedLocation,
                        StringComparison.OrdinalIgnoreCase) ||
                    normalizedLocation.Contains(
                        job.Location,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(jobType))
        {
            string normalizedJobType =
                jobType.Trim();

            filtered =
                filtered.Where(job =>
                    !string.IsNullOrWhiteSpace(
                        job.JobType) &&
                    job.JobType.Contains(
                        normalizedJobType,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (remote.HasValue)
        {
            filtered =
                filtered.Where(job =>
                    job.IsRemote ==
                    remote.Value);
        }

        return filtered.ToList();
    }

}