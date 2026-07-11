using System.Text;
using System.Text.Json;

namespace Dwuma.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;

public GeminiService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured.");
    }

    public async Task<string> GenerateAsync(
        string prompt,
        int maxTokens = 3000,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            contents = new[]
            {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = maxTokens,
                responseMimeType = "application/json"
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}",
            body,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini error: {Content}", content);
            throw new Exception("Failed to generate AI response.");
        }

        using var doc = JsonDocument.Parse(content);

        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

}
