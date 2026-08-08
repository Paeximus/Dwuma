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

            Source =
                "SerpApi",

            SourceUrl =
                applyUrl,

            Status =
                "Active",

            DiscoveredAt =
                DateTime.UtcNow
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
}