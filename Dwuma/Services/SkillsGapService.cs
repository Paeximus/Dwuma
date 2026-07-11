using System.Text;
using System.Text.Json;
using Dwuma.Models;

namespace Dwuma.Services;

public sealed class SkillsGapService
{
    private readonly GeminiService _geminiService;
    private readonly ILogger<SkillsGapService> _logger;

    public SkillsGapService(
        GeminiService geminiService,
        ILogger<SkillsGapService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<SkillsGapResponse> AnalyseAsync(
        SkillsGapRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        string prompt = BuildPrompt(request);

        string rawJson =
    await _geminiService.GenerateJsonAsync(
        prompt,
        responseSchema: CreateResponseSchema(),
        maxOutputTokens: 5000,
        cancellationToken: cancellationToken);

        return ParseResponse(rawJson);
    }

    private static void ValidateRequest(
        SkillsGapRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.JobTitle))
        {
            throw new ArgumentException(
                "The target job title is required.");
        }

        request.Skills ??= [];
    }

    private static string BuildPrompt(
        SkillsGapRequest request)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(
            "You are an expert career adviser familiar with graduate employment in Ghana.");

        prompt.AppendLine(
            "Analyse the candidate against the target role.");

        prompt.AppendLine(
            "Do not claim that a skill is present unless the candidate supplied evidence for it.");

        prompt.AppendLine();
        prompt.AppendLine("CANDIDATE PROFILE");
        prompt.AppendLine(
            $"Skills: {string.Join(", ", request.Skills)}");

        if (!string.IsNullOrWhiteSpace(request.Education))
        {
            prompt.AppendLine(
                $"Education: {request.Education}");
        }

        if (!string.IsNullOrWhiteSpace(request.FieldOfStudy))
        {
            prompt.AppendLine(
                $"Field of study: {request.FieldOfStudy}");
        }

        if (!string.IsNullOrWhiteSpace(request.Experience))
        {
            prompt.AppendLine(
                $"Experience: {request.Experience}");
        }

        prompt.AppendLine();
        prompt.AppendLine("TARGET ROLE");
        prompt.AppendLine($"Job title: {request.JobTitle}");

        if (!string.IsNullOrWhiteSpace(request.Industry))
        {
            prompt.AppendLine(
                $"Industry: {request.Industry}");
        }

        if (!string.IsNullOrWhiteSpace(
            request.JobDescription))
        {
            prompt.AppendLine("Job description:");
            prompt.AppendLine(request.JobDescription);
        }

        prompt.AppendLine();
        prompt.AppendLine("INSTRUCTIONS");
        prompt.AppendLine(
            "1. Identify the most important skills for the role.");

        prompt.AppendLine(
            "2. Mark every skill as present, partial, or absent.");

        prompt.AppendLine(
            "3. Provide a required level: foundational, intermediate, or advanced.");

        prompt.AppendLine(
            "4. Recommend practical learning resources for partial or absent skills.");

        prompt.AppendLine(
            "5. Prefer free resources or resources that can be audited for free.");

        prompt.AppendLine(
            "6. Calculate a realistic match percentage from 0 to 100.");

        prompt.AppendLine(
            "Return no more than 8 skill items and no more than 5 learning resources.");

        prompt.AppendLine(
            "Keep the summary under 80 words.");

        prompt.AppendLine(
            "Keep each skill description under 35 words.");

        prompt.AppendLine(
            "Keep each resource description under 30 words.");

        prompt.AppendLine(
            "7. Return valid JSON only.");

        prompt.AppendLine();
        prompt.AppendLine("RETURN THIS EXACT JSON STRUCTURE:");

        prompt.AppendLine(
            """
            {
              "matchPercentage": 72,
              "summary": "A brief honest and encouraging assessment.",
              "skills": [
                {
                  "name": "SQL",
                  "status": "present",
                  "level": "intermediate",
                  "description": "Explanation of the assessment."
                }
              ],
              "resources": [
                {
                  "name": "Resource name",
                  "platform": "Platform name",
                  "description": "Why this resource is useful.",
                  "url": "https://example.com",
                  "isFree": true
                }
              ]
            }
            """);

        return prompt.ToString();
    }

    private static object CreateResponseSchema()
    {
        return new
        {
            type = "object",

            properties = new
            {
                matchPercentage = new
                {
                    type = "integer",
                    minimum = 0,
                    maximum = 100
                },

                summary = new
                {
                    type = "string"
                },

                skills = new
                {
                    type = "array",
                    maxItems = 8,

                    items = new
                    {
                        type = "object",

                        properties = new
                        {
                            name = new
                            {
                                type = "string"
                            },

                            status = new
                            {
                                type = "string",

                                @enum = new[]
                                {
                                "present",
                                "partial",
                                "absent"
                            }
                            },

                            level = new
                            {
                                type = "string",

                                @enum = new[]
                                {
                                "foundational",
                                "intermediate",
                                "advanced"
                            }
                            },

                            description = new
                            {
                                type = "string"
                            }
                        },

                        required = new[]
                        {
                        "name",
                        "status",
                        "level",
                        "description"
                    }
                    }
                },

                resources = new
                {
                    type = "array",
                    maxItems = 5,

                    items = new
                    {
                        type = "object",

                        properties = new
                        {
                            name = new
                            {
                                type = "string"
                            },

                            platform = new
                            {
                                type = "string"
                            },

                            description = new
                            {
                                type = "string"
                            },

                            url = new
                            {
                                type = "string"
                            },

                            isFree = new
                            {
                                type = "boolean"
                            }
                        },

                        required = new[]
                        {
                        "name",
                        "platform",
                        "description",
                        "url",
                        "isFree"
                    }
                    }
                }
            },

            required = new[]
            {
            "matchPercentage",
            "summary",
            "skills",
            "resources"
        }
        };
    }

    private SkillsGapResponse ParseResponse(
    string rawJson)
    {
        try
        {
            string cleaned = ExtractJson(rawJson);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            SkillsGapResponse? result =
                JsonSerializer.Deserialize<SkillsGapResponse>(
                    cleaned,
                    options);

            if (result is null)
            {
                throw new InvalidOperationException(
                    "Gemini returned an empty skills-gap result.");
            }

            result.Skills ??= [];
            result.Resources ??= [];

            result.MatchPercentage = Math.Clamp(
                result.MatchPercentage,
                0,
                100);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Unable to parse skills-gap JSON: {RawJson}",
                rawJson);

            throw new InvalidOperationException(
                "The AI returned an invalid skills-gap response.",
                ex);
        }
    }

    private static string ExtractJson(
        string raw)
    {
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
                "No JSON object was found.");
        }

        return cleaned.Substring(
            start,
            end - start + 1);
    }
}