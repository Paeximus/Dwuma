using Dwuma.Models.Interview;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dwuma.Services;

public sealed class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly IReadOnlyList<string> _models;

    private const int MaximumAttemptsPerModel = 3;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey =
            configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Gemini API key is not configured.");

        string[] configuredModels =
            configuration
                .GetSection("Gemini:Models")
                .Get<string[]>()
            ?? [];

        _models =
            configuredModels.Length > 0
                ? configuredModels
                : new[]
                {
                        "gemini-3.6-flash",
                        "gemini-3.5-flash-lite",
                        "gemini-3.5-flash"
                };
    }

    public async Task<string> GenerateJsonAsync(
        string prompt,
        object? responseSchema = null,
        int maxOutputTokens = 3000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException(
                "Prompt cannot be empty.",
                nameof(prompt));
        }

        Exception? lastException = null;

        foreach (string model in _models)
        {
            for (
                int attempt = 1;
                attempt <= MaximumAttemptsPerModel;
                attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Calling Gemini model {Model}. Attempt {Attempt}/{MaximumAttempts}.",
                        model,
                        attempt,
                        MaximumAttemptsPerModel);

                    return await SendJsonRequestAsync(
                        model,
                        prompt,
                        responseSchema,
                        maxOutputTokens,
                        cancellationToken);
                }
                catch (GeminiRateLimitException ex)
                {
                    lastException = ex;

                    _logger.LogWarning(
                        ex,
                        "Gemini model {Model} reached a temporary limit on attempt {Attempt}.",
                        model,
                        attempt);

                    if (attempt == MaximumAttemptsPerModel)
                    {
                        break;
                    }

                    TimeSpan delay =
                        ex.RetryAfter ??
                        CalculateRetryDelay(attempt);

                    await Task.Delay(
                        delay,
                        cancellationToken);
                }
                catch (GeminiTemporaryException ex)
                {
                    lastException = ex;

                    _logger.LogWarning(
                        ex,
                        "Gemini model {Model} is temporarily unavailable on attempt {Attempt}.",
                        model,
                        attempt);

                    if (attempt == MaximumAttemptsPerModel)
                    {
                        break;
                    }

                    await Task.Delay(
                        CalculateRetryDelay(attempt),
                        cancellationToken);
                }
                catch (GeminiRequestException ex)
                {
                    // Invalid request, authentication failure,
                    // unsupported schema, and similar errors should
                    // not be repeated against the same model.
                    lastException = ex;

                    _logger.LogError(
                        ex,
                        "Gemini model {Model} rejected the request.",
                        model);

                    break;
                }
            }

            _logger.LogWarning(
                "Switching from Gemini model {Model} to the next configured model.",
                model);
        }

        throw new InvalidOperationException(
            "All configured Gemini models are currently unavailable or have reached their limits.",
            lastException);
    }

    public async Task<string> TranscribeAudioAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        if (audioBytes is null || audioBytes.Length == 0)
        {
            throw new ArgumentException(
                "Audio data cannot be empty.",
                nameof(audioBytes));
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            mimeType = "audio/webm";
        }

        Exception? lastException = null;

        foreach (string model in _models)
        {
            for (
                int attempt = 1;
                attempt <= MaximumAttemptsPerModel;
                attempt++)
            {
                try
                {
                    return await SendAudioRequestAsync(
                        model,
                        audioBytes,
                        mimeType,
                        cancellationToken);
                }
                catch (GeminiRateLimitException ex)
                {
                    lastException = ex;

                    if (attempt == MaximumAttemptsPerModel)
                    {
                        break;
                    }

                    await Task.Delay(
                        ex.RetryAfter ??
                        CalculateRetryDelay(attempt),
                        cancellationToken);
                }
                catch (GeminiTemporaryException ex)
                {
                    lastException = ex;

                    if (attempt == MaximumAttemptsPerModel)
                    {
                        break;
                    }

                    await Task.Delay(
                        CalculateRetryDelay(attempt),
                        cancellationToken);
                }
                catch (GeminiRequestException ex)
                {
                    lastException = ex;
                    break;
                }
            }
        }

        throw new InvalidOperationException(
            "Audio transcription is currently unavailable because all configured Gemini models failed.",
            lastException);
    }

    private async Task<string> SendJsonRequestAsync(
        string model,
        string prompt,
        object? responseSchema,
        int maxOutputTokens,
        CancellationToken cancellationToken)
    {
        object generationConfig =
            responseSchema is null
                ? new
                {
                    temperature = 0.2,
                    maxOutputTokens,
                    responseMimeType =
                        "application/json"
                }
                : new
                {
                    temperature = 0.2,
                    maxOutputTokens,
                    responseMimeType =
                        "application/json",
                    responseJsonSchema =
                        responseSchema
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
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },
            generationConfig
        };

        using var request =
            CreateRequest(
                model,
                requestBody);

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        await EnsureSuccessfulResponseAsync(
            response,
            responseBody);

        return ExtractGeneratedText(
            responseBody);
    }

    private async Task<string> SendVideoRequestAsync(
    string model,
    byte[] videoBytes,
    string contentType,
    string prompt,
    CancellationToken cancellationToken)
    {
        string base64Video =
            Convert.ToBase64String(
                videoBytes);

        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                role = "user",

                parts = new object[]
                {
                    new
                    {
                        text = prompt
                    },

                    new
                    {
                        inlineData = new
                        {
                            mimeType =
                                contentType,

                            data =
                                base64Video
                        }
                    }
                }
            }
        },
            generationConfig = new
            {
                temperature = 0.0,

                maxOutputTokens = 6000,

                responseMimeType =
        "application/json",

                responseJsonSchema = new
                {
                    type = "object",

                    properties = new
                    {
                        answers = new
                        {
                            type = "array",

                            minItems = 1,

                            items = new
                            {
                                type = "object",

                                properties = new
                                {
                                    questionId = new
                                    {
                                        type = "integer"
                                    },

                                    questionNumber = new
                                    {
                                        type = "integer"
                                    },

                                    question = new
                                    {
                                        type = "string"
                                    },

                                    transcript = new
                                    {
                                        type = "string"
                                    },

                                    answerStartedAt = new
                                    {
                                        type = "number"
                                    },

                                    answerEndedAt = new
                                    {
                                        type = "number"
                                    }
                                },

                                required = new[]
                    {
                        "questionId",
                        "questionNumber",
                        "question",
                        "transcript",
                        "answerStartedAt",
                        "answerEndedAt"
                    }
                            }
                        }
                    },

                    required = new[]
        {
            "answers"
        }
                }
            }
        };

        using var request =
            CreateRequest(
                model,
                requestBody);

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        string responseBody =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        await EnsureSuccessfulResponseAsync(
            response,
            responseBody);

        return ExtractGeneratedText(
            responseBody);
    }

    private async Task<string> SendAudioRequestAsync(
        string model,
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken)
    {
        string base64Audio =
            Convert.ToBase64String(audioBytes);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            text =
                                "Transcribe this interview answer accurately. " +
                                "Return only the spoken words as plain text. " +
                                "Do not add commentary or markdown."
                        },
                        new
                        {
                            inlineData = new
                            {
                                mimeType,
                                data = base64Audio
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.0,
                maxOutputTokens = 1500
            }
        };

        using var request =
            CreateRequest(
                model,
                requestBody);

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        await EnsureSuccessfulResponseAsync(
            response,
            responseBody);

        return ExtractGeneratedText(
            responseBody);
    }

    private HttpRequestMessage CreateRequest(
        string model,
        object requestBody)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "https://generativelanguage.googleapis.com/" +
                $"v1beta/models/{model}:generateContent");

        request.Headers.Add(
            "x-goog-api-key",
            _apiKey);

        request.Content =
            JsonContent.Create(requestBody);

        return request;
    }

    private async Task EnsureSuccessfulResponseAsync(
        HttpResponseMessage response,
        string responseBody)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        _logger.LogError(
            "Gemini failed with status {StatusCode}. Body: {Body}",
            response.StatusCode,
            responseBody);

        TimeSpan? retryAfter =
            GetRetryAfter(response);

        if (response.StatusCode ==
            HttpStatusCode.TooManyRequests)
        {
            throw new GeminiRateLimitException(
                "Gemini rate limit was reached.",
                retryAfter);
        }

        if (response.StatusCode ==
                HttpStatusCode.ServiceUnavailable ||
            response.StatusCode ==
                HttpStatusCode.InternalServerError ||
            response.StatusCode ==
                HttpStatusCode.BadGateway ||
            response.StatusCode ==
                HttpStatusCode.GatewayTimeout)
        {
            throw new GeminiTemporaryException(
                $"Gemini temporarily failed with status {(int)response.StatusCode}.");
        }

        string errorMessage =
            await ExtractErrorMessageAsync(
                responseBody);

        throw new GeminiRequestException(
            $"Gemini rejected the request with status " +
            $"{(int)response.StatusCode}: {errorMessage}");
    }

    private static string ExtractGeneratedText(
        string responseBody)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(responseBody);

            JsonElement root =
                document.RootElement;

            if (!root.TryGetProperty(
                    "candidates",
                    out JsonElement candidates) ||
                candidates.ValueKind !=
                    JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                throw new GeminiRequestException(
                    "Gemini returned no candidates.");
            }

            JsonElement candidate =
                candidates[0];

            string? finishReason =
                candidate.TryGetProperty(
                    "finishReason",
                    out JsonElement finishElement)
                    ? finishElement.GetString()
                    : null;

            if (string.Equals(
                finishReason,
                "MAX_TOKENS",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiRequestException(
                    "Gemini stopped because maxOutputTokens was reached. " +
                    "Reduce the response size or increase the output limit.");
            }

            if (!candidate.TryGetProperty(
                    "content",
                    out JsonElement content) ||
                !content.TryGetProperty(
                    "parts",
                    out JsonElement parts) ||
                parts.ValueKind !=
                    JsonValueKind.Array ||
                parts.GetArrayLength() == 0)
            {
                throw new GeminiRequestException(
                    $"Gemini returned no content. Finish reason: " +
                    $"{finishReason ?? "unknown"}.");
            }

            var textBuilder =
                new System.Text.StringBuilder();

            foreach (JsonElement part in
                     parts.EnumerateArray())
            {
                if (part.TryGetProperty(
                        "text",
                        out JsonElement textElement))
                {
                    textBuilder.Append(
                        textElement.GetString());
                }
            }

            string text =
                textBuilder.ToString().Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new GeminiRequestException(
                    "Gemini returned empty text.");
            }

            return text;
        }
        catch (JsonException ex)
        {
            throw new GeminiRequestException(
                "Gemini returned malformed JSON.",
                ex);
        }
    }

    private static TimeSpan? GetRetryAfter(
        HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta
            is TimeSpan delta)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date
            is DateTimeOffset retryDate)
        {
            TimeSpan delay =
                retryDate - DateTimeOffset.UtcNow;

            return delay > TimeSpan.Zero
                ? delay
                : TimeSpan.FromSeconds(2);
        }

        return null;
    }

    private static TimeSpan CalculateRetryDelay(
        int attempt)
    {
        // 2 seconds, 4 seconds, then 8 seconds.
        int seconds =
            (int)Math.Pow(2, attempt);

        // Add small jitter so simultaneous requests
        // do not all retry at the same instant.
        int jitterMilliseconds =
            Random.Shared.Next(100, 800);

        return TimeSpan.FromMilliseconds(
            seconds * 1000 +
            jitterMilliseconds);
    }

    private static Task<string> ExtractErrorMessageAsync(
        string responseBody)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(responseBody);

            if (document.RootElement.TryGetProperty(
                    "error",
                    out JsonElement error) &&
                error.TryGetProperty(
                    "message",
                    out JsonElement message))
            {
                return Task.FromResult(
                    message.GetString() ??
                    "Unknown Gemini error.");
            }
        }
        catch (JsonException)
        {
            // Fall back to the raw response below.
        }

        string safeMessage =
            string.IsNullOrWhiteSpace(responseBody)
                ? "No error details were returned."
                : responseBody.Length > 500
                    ? responseBody[..500]
                    : responseBody;

        return Task.FromResult(safeMessage);
    }

    public async Task<string> AnalyzeVideoAsync(
    byte[] videoBytes,
    string contentType,
    string prompt,
    CancellationToken cancellationToken = default)
{
    if (videoBytes is null ||
        videoBytes.Length == 0)
    {
        throw new ArgumentException(
            "Video data cannot be empty.",
            nameof(videoBytes));
    }

    if (string.IsNullOrWhiteSpace(contentType))
    {
        contentType = "video/webm";
    }

    if (string.IsNullOrWhiteSpace(prompt))
    {
        throw new ArgumentException(
            "Video analysis prompt cannot be empty.",
            nameof(prompt));
    }

    Exception? lastException = null;

    foreach (string model in _models)
    {
        for (
            int attempt = 1;
            attempt <= MaximumAttemptsPerModel;
            attempt++)
        {
            try
            {
                return await SendVideoRequestAsync(
                    model,
                    videoBytes,
                    contentType,
                    prompt,
                    cancellationToken);
            }
            catch (GeminiRateLimitException ex)
            {
                lastException = ex;

                if (attempt ==
                    MaximumAttemptsPerModel)
                {
                    break;
                }

                await Task.Delay(
                    ex.RetryAfter ??
                    CalculateRetryDelay(attempt),
                    cancellationToken);
            }
            catch (GeminiTemporaryException ex)
            {
                lastException = ex;

                if (attempt ==
                    MaximumAttemptsPerModel)
                {
                    break;
                }

                await Task.Delay(
                    CalculateRetryDelay(attempt),
                    cancellationToken);
            }
            catch (GeminiRequestException ex)
            {
                lastException = ex;
                break;
            }
        }
    }

    throw new InvalidOperationException(
        "Video analysis is currently unavailable because all configured Gemini models failed.",
        lastException);
}

}

public sealed class GeminiRateLimitException
    : Exception
{
    public TimeSpan? RetryAfter { get; }

    public GeminiRateLimitException(
        string message,
        TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }
}

public sealed class GeminiTemporaryException
    : Exception
{
    public GeminiTemporaryException(
        string message)
        : base(message)
    {
    }
}

public sealed class GeminiRequestException
    : Exception
{
    public GeminiRequestException(
        string message)
        : base(message)
    {
    }

    public GeminiRequestException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}



    