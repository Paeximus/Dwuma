using System.Net.Http.Json;
using System.Text.Json;

namespace Dwuma.Services;

public sealed class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Gemini API key is not configured.");

        _model = configuration["Gemini:Model"]
            ?? "gemini-2.5-flash";
    }

    public async Task<string> GenerateJsonAsync(
        string prompt,
        object? responseSchema = null,
        int maxOutputTokens = 5000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException(
                "Prompt cannot be empty.",
                nameof(prompt));
        }

        object generationConfig = responseSchema is null
            ? new
            {
                temperature = 0.2,
                maxOutputTokens,
                responseMimeType = "application/json"
            }
            : new
            {
                temperature = 0.2,
                maxOutputTokens,
                responseMimeType = "application/json",
                responseJsonSchema = responseSchema
            };

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent");

        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = JsonContent.Create(requestBody);

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Gemini API failed with status {StatusCode}. Body: {Body}",
                response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"Gemini request failed with status {(int)response.StatusCode}.");
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(responseBody);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty(
                    "candidates",
                    out JsonElement candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Gemini returned no candidates.");
            }

            JsonElement candidate = candidates[0];

            string? finishReason =
                candidate.TryGetProperty(
                    "finishReason",
                    out JsonElement finishReasonElement)
                    ? finishReasonElement.GetString()
                    : null;

            if (string.Equals(
                finishReason,
                "MAX_TOKENS",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Gemini response was truncated because the output-token limit was reached.");
            }

            if (!candidate.TryGetProperty(
                    "content",
                    out JsonElement content) ||
                !content.TryGetProperty(
                    "parts",
                    out JsonElement parts) ||
                parts.ValueKind != JsonValueKind.Array ||
                parts.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    $"Gemini returned no content. Finish reason: {finishReason ?? "unknown"}.");
            }

            string? text =
                parts[0]
                    .GetProperty("text")
                    .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException(
                    $"Gemini returned empty text. Finish reason: {finishReason ?? "unknown"}.");
            }

            return text;
        }
        catch (Exception ex) when (
            ex is JsonException ||
            ex is KeyNotFoundException ||
            ex is InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Could not parse Gemini response: {Body}",
                responseBody);

            throw new InvalidOperationException(
                "Gemini returned an unexpected response.",
                ex);
        }
    }
}