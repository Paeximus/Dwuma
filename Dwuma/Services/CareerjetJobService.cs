using Azure;
using Dwuma.Models.Jobs;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Dwuma.Services;

public sealed class CareerjetJobService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CareerjetJobService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public CareerjetJobService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CareerjetJobService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GhanaJobSearchResponse> SearchAsync(
        GhanaJobSearchRequest request,
        string userIp,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        string apiKey =
            _configuration["Careerjet:ApiKey"]
            ?? throw new InvalidOperationException(
                "Careerjet API key is not configured.");

        string baseUrl =
            _configuration["Careerjet:BaseUrl"]
            ?? "https://search.api.careerjet.net/v4/query";

        string localeCode =
            _configuration["Careerjet:LocaleCode"]
            ?? "en_GB";

        string location =
            string.IsNullOrWhiteSpace(request.Location)
                ? "Ghana"
                : request.Location.Trim();

        var parameters =
            new Dictionary<string, string?>
            {
                ["locale_code"] = localeCode,
                ["keywords"] = request.Keywords?.Trim(),
                ["location"] = location,
                ["page"] = request.Page.ToString(),
                ["page_size"] = request.PageSize.ToString(),
                ["sort"] = "date",
                ["user_ip"] = userIp,
                ["user_agent"] = userAgent
            };

        string requestUrl =
            QueryHelpers.AddQueryString(
                baseUrl,
                parameters);

        string credentials =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{apiKey}:"));

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestUrl);

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                credentials);

        using HttpResponseMessage response =
        await _httpClient.SendAsync(
        httpRequest,
        cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (response.StatusCode ==
            System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(
                "Careerjet rejected the request from the current server IP. Response: {Response}",
                responseBody);

            throw new UnauthorizedAccessException(
                "Careerjet rejected this server's public IP address. " +
                "Add the current public IP to the Careerjet publisher whitelist.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Careerjet failed. Status: {StatusCode}. Reason: {ReasonPhrase}. Response: {Response}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                responseBody);

            throw new HttpRequestException(
                $"Careerjet failed with HTTP {(int)response.StatusCode}.");
        }

        CareerjetSearchResponse? result =
            JsonSerializer.Deserialize<CareerjetSearchResponse>(
                responseBody,
                JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException(
                "Careerjet returned an empty response.");
        }

        if (string.Equals(
                result.Type,
                "LOCATIONS",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Careerjet could not resolve the requested location."
                    : result.Message);
        }
        List<GhanaJobResult> jobs =
            result.Jobs
                .Select(job =>
                    new GhanaJobResult
                    {
                        ExternalId =
                            Convert.ToHexString(
                                System.Security.Cryptography.SHA256.HashData(
                                    Encoding.UTF8.GetBytes(job.ApplyUrl))),

                        Title = job.Title,
                        Company = job.Company,
                        Location = job.Location,
                        Description = job.Description,
                        Salary = job.Salary,
                        JobType = string.Empty,
                        Source =
                            string.IsNullOrWhiteSpace(job.Source)
                                ? "Careerjet"
                                : job.Source,
                        ApplyUrl = job.ApplyUrl,
                        UpdatedAt =
                            DateTime.TryParse(
                                job.Date,
                                out DateTime parsedDate)
                                ? parsedDate
                                : null
                    })
                .Where(job =>
                    !string.IsNullOrWhiteSpace(job.Title) &&
                    !string.IsNullOrWhiteSpace(job.ApplyUrl))
                .ToList();

        return new GhanaJobSearchResponse
        {
            ProviderTotalCount = result.Hits,
            ReturnedCount = jobs.Count,
            Page = request.Page,
            PageSize = request.PageSize,
            Location = location,
            Jobs = jobs
        };
    }
}