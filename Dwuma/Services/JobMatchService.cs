using System.Text;
using System.Text.Json;
using Dwuma.Models.JobMatching;

namespace Dwuma.Services;

public sealed class JobMatchService
{
    private readonly GeminiService _geminiService;
    private readonly ILogger<JobMatchService> _logger;

    public JobMatchService(
        GeminiService geminiService,
        ILogger<JobMatchService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<JobMatchResponse> AnalyseAsync(
        JobMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        string prompt = BuildPrompt(request);

        string rawJson =
            await _geminiService.GenerateJsonAsync(
                prompt,
                responseSchema: CreateResponseSchema(),
                maxOutputTokens: 3500,
                cancellationToken: cancellationToken);

        JobMatchResponse response =
            ParseResponse(rawJson);

        response.CompatibilityScore = Math.Clamp(
            response.CompatibilityScore,
            0,
            100);

        response.MatchingSkills ??= [];
        response.MissingSkills ??= [];
        response.Strengths ??= [];
        response.Concerns ??= [];

        return response;
    }

    private static void ValidateRequest(
        JobMatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(
            request.JobTitle))
        {
            throw new ArgumentException(
                "The job title is required.");
        }

        if (string.IsNullOrWhiteSpace(
            request.JobDescription))
        {
            throw new ArgumentException(
                "The job description is required.");
        }

        request.CandidateSkills ??= [];
    }

    private static string BuildPrompt(
        JobMatchRequest request)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(
            "You are an experienced recruitment and career-matching adviser.");

        prompt.AppendLine(
            "Evaluate how well the candidate matches the job.");

        prompt.AppendLine(
            "Use only the supplied candidate information.");

        prompt.AppendLine(
            "Do not invent qualifications, skills, experience, or achievements.");

        prompt.AppendLine(
            "Be realistic and fair to graduate and entry-level candidates.");

        prompt.AppendLine();

        prompt.AppendLine("JOB INFORMATION");

        prompt.AppendLine(
            $"Job title: {request.JobTitle}");

        prompt.AppendLine(
            $"Company: {Fallback(request.CompanyName)}");

        prompt.AppendLine(
            $"Location: {Fallback(request.JobLocation)}");

        prompt.AppendLine("Job description:");

        prompt.AppendLine(
            request.JobDescription);

        prompt.AppendLine();

        prompt.AppendLine("CANDIDATE INFORMATION");

        prompt.AppendLine(
            $"Skills: {FormatList(request.CandidateSkills)}");

        prompt.AppendLine(
            $"Education: {Fallback(request.CandidateEducation)}");

        prompt.AppendLine(
            $"Experience: {Fallback(request.CandidateExperience)}");

        prompt.AppendLine(
            $"Preferred location: {Fallback(request.PreferredLocation)}");

        prompt.AppendLine(
            $"Career goals: {Fallback(request.CareerGoals)}");

        prompt.AppendLine();

        prompt.AppendLine("INSTRUCTIONS");

        prompt.AppendLine(
            "Give a compatibility score from 0 to 100.");

        prompt.AppendLine(
            "Consider skills, experience, education, role seniority, and location.");

        prompt.AppendLine(
            "Recommendation must be one of: strong_match, good_match, possible_match, weak_match.");

        prompt.AppendLine(
            "List no more than six matching skills.");

        prompt.AppendLine(
            "List no more than six missing skills.");

        prompt.AppendLine(
            "List no more than four strengths and four concerns.");

        prompt.AppendLine(
            "Give practical advice on whether and how the candidate should apply.");

        prompt.AppendLine(
            "Keep the summary under 100 words.");

        prompt.AppendLine(
            "Return valid JSON only.");

        return prompt.ToString();
    }

    private JobMatchResponse ParseResponse(
        string rawJson)
    {
        try
        {
            string cleaned = ExtractJson(rawJson);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            JobMatchResponse? result =
                JsonSerializer.Deserialize<JobMatchResponse>(
                    cleaned,
                    options);

            return result
                ?? throw new InvalidOperationException(
                    "Gemini returned an empty job-match result.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Unable to parse job-match response: {RawJson}",
                rawJson);

            throw new InvalidOperationException(
                "The AI returned an invalid job-match response.",
                ex);
        }
    }

    private static string ExtractJson(
        string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException(
                "The AI response was empty.");
        }

        string cleaned = raw.Trim();

        if (cleaned.StartsWith("```"))
        {
            int firstNewLine =
                cleaned.IndexOf('\n');

            if (firstNewLine >= 0)
            {
                cleaned =
                    cleaned[(firstNewLine + 1)..];
            }
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned[..^3];
        }

        int start = cleaned.IndexOf('{');
        int end = cleaned.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            throw new JsonException(
                "No complete JSON object was found.");
        }

        return cleaned.Substring(
            start,
            end - start + 1);
    }

    private static string Fallback(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Not provided"
            : value.Trim();
    }

    private static string FormatList(
        IEnumerable<string>? values)
    {
        if (values is null)
        {
            return "None provided";
        }

        string[] cleanedValues =
            values
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToArray();

        return cleanedValues.Length == 0
            ? "None provided"
            : string.Join(", ", cleanedValues);
    }

    private static object CreateResponseSchema()
    {
        return new
        {
            type = "object",

            properties = new
            {
                compatibilityScore = new
                {
                    type = "integer",
                    minimum = 0,
                    maximum = 100
                },

                recommendation = new
                {
                    type = "string",
                    @enum = new[]
                    {
                        "strong_match",
                        "good_match",
                        "possible_match",
                        "weak_match"
                    }
                },

                summary = new
                {
                    type = "string"
                },

                matchingSkills = new
                {
                    type = "array",
                    maxItems = 6,
                    items = new
                    {
                        type = "string"
                    }
                },

                missingSkills = new
                {
                    type = "array",
                    maxItems = 6,
                    items = new
                    {
                        type = "string"
                    }
                },

                strengths = new
                {
                    type = "array",
                    maxItems = 4,
                    items = new
                    {
                        type = "string"
                    }
                },

                concerns = new
                {
                    type = "array",
                    maxItems = 4,
                    items = new
                    {
                        type = "string"
                    }
                },

                applicationAdvice = new
                {
                    type = "string"
                }
            },

            required = new[]
            {
                "compatibilityScore",
                "recommendation",
                "summary",
                "matchingSkills",
                "missingSkills",
                "strengths",
                "concerns",
                "applicationAdvice"
            }
        };
    }
}