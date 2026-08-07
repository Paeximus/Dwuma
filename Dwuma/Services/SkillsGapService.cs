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
        ArgumentNullException.ThrowIfNull(request);

        if (request.Skills is null ||
            request.Skills.Count == 0)
        {
            throw new ArgumentException(
                "At least one skill is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.FieldOfStudy))
        {
            throw new ArgumentException(
                "Field of work is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.JobTitle))
        {
            throw new ArgumentException(
                "Role is required.");
        }

        List<string> skills =
            request.Skills
                .Where(skill =>
                    !string.IsNullOrWhiteSpace(skill))
                .Select(skill => skill.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (skills.Count == 0)
        {
            throw new ArgumentException(
                "At least one valid skill is required.");
        }

        string skillsText =
            string.Join(
                ", ",
                skills);

        string prompt = $"""
        You are an expert career adviser familiar with
        graduate employment and professional skills.

        Analyse the candidate's skills against the
        skills normally required for the target role.

        CURRENT SKILLS:
        {skillsText}

        FIELD OF WORK:
        {request.FieldOfStudy.Trim()}

        TARGET ROLE:
        {request.JobTitle.Trim()}

        Determine the skill gap between the
        candidate's current skills and the skills
        required for the target role.

        Requirements:

        - Return at most 8 important skills.
        - For every skill, classify the candidate as:
          present, partial, or absent.
        - Explain briefly why each skill matters.
        - Identify the most important missing skills.
        - Recommend at most 5 learning resources.
        - Prefer free learning resources.
        - Keep the final summary below 80 words.
        - Return valid JSON only.
        - Do not use markdown.
        - Do not use code fences.
        """;

        string response =
            await _geminiService.GenerateJsonAsync(
                prompt,
                responseSchema:
                    CreateResponseSchema(),
                maxOutputTokens: 5000,
                cancellationToken:
                    cancellationToken);

        SkillsGapResponse result =
            ParseResponse(response);

        return result;
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