using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Jobs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services;

public sealed class CareerjetJobService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly DwumaContext _db;
    private readonly ILogger<CareerjetJobService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public CareerjetJobService(
        HttpClient httpClient,
        IConfiguration configuration,
        DwumaContext db,
        ILogger<CareerjetJobService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _db = db;
        _logger = logger;
    }

    public async Task<GhanaJobSearchResponse> SearchAsync(
    GhanaJobSearchRequest request,
    string userIp,
    string userAgent,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string apiKey =
            _configuration["Careerjet:ApiKey"]
            ?? throw new InvalidOperationException(
                "Careerjet API key is not configured.");

        string baseUrl =
            _configuration["Careerjet:BaseUrl"]
            ?? "https://search.api.careerjet.net/v4/query";

        string localeCode =
            _configuration["Careerjet:LocaleCode"]
            ?? "en_GH";

        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);

        string keywords =
            string.IsNullOrWhiteSpace(request.Keywords)
                ? "jobs"
                : request.Keywords.Trim();

        string location =
            string.IsNullOrWhiteSpace(request.Location)
                ? "Ghana"
                : request.Location.Trim();

        var queryParameters =
            new Dictionary<string, string?>
            {
                ["locale_code"] = localeCode,
                ["keywords"] = keywords,
                ["location"] = location,
                ["page"] = page.ToString(),
                ["page_size"] = pageSize.ToString(),
                ["sort"] = "date",
                ["user_ip"] = userIp,
                ["user_agent"] = userAgent
            };

        string requestUrl =
            QueryHelpers.AddQueryString(
                baseUrl,
                queryParameters);

        string credentials =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{apiKey}:"));

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestUrl);

        httpRequest.Headers.Referrer =
            new Uri("https://dwuma-api.onrender.com");

        _logger.LogInformation(
            "Careerjet request. Location: {Location}; UserIp: {UserIp}; UserAgentPresent: {HasUserAgent}",
            location,
            userIp,
            !string.IsNullOrWhiteSpace(userAgent));

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                credentials);

        httpRequest.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

        string body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Careerjet failed with HTTP {StatusCode}. Body: {Body}",
                response.StatusCode,
                body);

            throw new HttpRequestException(
                $"Careerjet returned HTTP {(int)response.StatusCode}.");
        }

        CareerjetSearchResponse? result =
            JsonSerializer.Deserialize<CareerjetSearchResponse>(
                body,
                JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException(
                "Careerjet returned an empty response.");
        }

        await CacheJobsAsync(
            result.Jobs,
            cancellationToken);

        List<GhanaJobResult> jobs =
            result.Jobs
                .Where(job =>
                    !string.IsNullOrWhiteSpace(job.Title) &&
                    !string.IsNullOrWhiteSpace(job.Url))
                .Select(MapResult)
                .ToList();

        return new GhanaJobSearchResponse
        {
            ProviderTotalCount = result.Hits,
            ReturnedCount = jobs.Count,
            Page = page,
            PageSize = pageSize,
            Location = location,
            Jobs = jobs
        };
    }

    private static GhanaJobResult MapResult(
        CareerjetJob job)
    {
        return new GhanaJobResult
        {
            ExternalId =
                GenerateExternalId(job),

            Title =
                job.Title?.Trim()
                ?? string.Empty,

            Company =
                job.Company?.Trim()
                ?? string.Empty,

            Location =
                job.Location?.Trim()
                ?? string.Empty,

            Description =
                job.Description?.Trim()
                ?? string.Empty,

            Salary =
                job.Salary?.Trim()
                ?? string.Empty,

            JobType =
                string.Empty,

            Source =
                "Careerjet",

            ApplyUrl =
                job.Url?.Trim()
                ?? string.Empty,

            UpdatedAt =
                ParseDate(job.Date)
        };
    }

    private async Task CacheJobsAsync(
        IEnumerable<CareerjetJob> providerJobs,
        CancellationToken cancellationToken)
    {
        foreach (CareerjetJob providerJob in providerJobs)
        {
            string externalId =
                GenerateExternalId(providerJob);

            JobListing? existing =
                await _db.JobListings
                    .FirstOrDefaultAsync(
                        job =>
                            job.Source == "Careerjet" &&
                            job.ExternalId == externalId,
                        cancellationToken);

            if (existing is null)
            {
                _db.JobListings.Add(
                    new JobListing
                    {
                        ExternalId = externalId,
                        Title =
                            providerJob.Title?.Trim()
                            ?? string.Empty,
                        Company =
                            providerJob.Company?.Trim(),
                        Location =
                            providerJob.Location?.Trim(),
                        Description =
                            providerJob.Description?.Trim(),
                        Salary =
                            providerJob.Salary?.Trim(),
                        Source = "Careerjet",
                        SourceUrl =
                            providerJob.Url?.Trim(),
                        PostedAt =
                            ParseDate(providerJob.Date),
                        DiscoveredAt =
                            DateTime.UtcNow,
                        Status = "Active",
                        IsRemote =
                            ContainsRemoteText(
                                providerJob.Title,
                                providerJob.Location,
                                providerJob.Description)
                    });

                continue;
            }

            existing.Title =
                providerJob.Title?.Trim()
                ?? existing.Title;

            existing.Company =
                providerJob.Company?.Trim();

            existing.Location =
                providerJob.Location?.Trim();

            existing.Description =
                providerJob.Description?.Trim();

            existing.Salary =
                providerJob.Salary?.Trim();

            existing.SourceUrl =
                providerJob.Url?.Trim();

            existing.PostedAt =
                ParseDate(providerJob.Date);

            existing.DiscoveredAt =
                DateTime.UtcNow;

            existing.Status =
                "Active";
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    private static string GenerateExternalId(
        CareerjetJob job)
    {
        string value =
            $"{job.Title}|" +
            $"{job.Company}|" +
            $"{job.Location}|" +
            $"{job.Url}";

        byte[] hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(hash);
    }

    private static DateTime? ParseDate(
        string? value)
    {
        return DateTime.TryParse(
            value,
            out DateTime date)
                ? date.ToUniversalTime()
                : null;
    }

    private static bool ContainsRemoteText(
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
}