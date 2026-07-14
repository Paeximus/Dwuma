using System.Text;
using System.Text.Json;
using Dwuma.Models.Interview;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Dwuma.Services;

public sealed class InterviewCoachService
{
    private readonly GeminiService _geminiService;
    private readonly ILogger<InterviewCoachService> _logger;

    public InterviewCoachService(
        GeminiService geminiService,
        ILogger<InterviewCoachService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<InterviewQuestionResponse> GenerateQuestionsAsync(
        InterviewQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateQuestionRequest(request);

        request.NumberOfQuestions = Math.Clamp(
            request.NumberOfQuestions,
            1,
            10);

        string prompt = BuildQuestionPrompt(request);

        string rawJson =
            await _geminiService.GenerateJsonAsync(
                prompt,
                responseSchema: CreateQuestionSchema(),
                maxOutputTokens: 4000,
                cancellationToken: cancellationToken);

        return ParseJson<InterviewQuestionResponse>(
            rawJson,
            "interview questions");
    }

    public async Task<InterviewFeedbackResponse> EvaluateAnswerAsync(
        InterviewAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAnswerRequest(request);

        string prompt = BuildFeedbackPrompt(request);

        string rawJson =
            await _geminiService.GenerateJsonAsync(
                prompt,
                responseSchema: CreateFeedbackSchema(),
                maxOutputTokens: 3500,
                cancellationToken: cancellationToken);

        InterviewFeedbackResponse response =
            ParseJson<InterviewFeedbackResponse>(
                rawJson,
                "interview feedback");

        response.Score = Math.Clamp(
            response.Score,
            0,
            100);

        response.Strengths ??= [];
        response.Improvements ??= [];

        return response;
    }

    private static void ValidateQuestionRequest(
        InterviewQuestionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.JobTitle))
        {
            throw new ArgumentException(
                "The job title is required.");
        }
    }

    private static void ValidateAnswerRequest(
        InterviewAnswerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new ArgumentException(
                "The interview question is required.");
        }

        if (string.IsNullOrWhiteSpace(
            request.CandidateAnswer))
        {
            throw new ArgumentException(
                "The candidate answer is required.");
        }
    }

    private static string BuildQuestionPrompt(
        InterviewQuestionRequest request)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(
            "You are a professional interview coach.");

        prompt.AppendLine(
            "Generate realistic interview questions for the candidate.");

        prompt.AppendLine(
            "Include a balanced mixture of technical, behavioural, situational, and role-specific questions.");

        prompt.AppendLine(
            "Do not invent private information about the company.");

        prompt.AppendLine();
        prompt.AppendLine("ROLE INFORMATION");
        prompt.AppendLine(
            $"Job title: {request.JobTitle}");

        prompt.AppendLine(
            $"Company: {Fallback(request.CompanyName)}");

        prompt.AppendLine(
            $"Job description: {Fallback(request.JobDescription)}");

        prompt.AppendLine();
        prompt.AppendLine("CANDIDATE INFORMATION");

        prompt.AppendLine(
            $"Skills: {Fallback(request.CandidateSkills)}");

        prompt.AppendLine(
            $"Experience: {Fallback(request.CandidateExperience)}");

        prompt.AppendLine();
        prompt.AppendLine(
            $"Generate exactly {request.NumberOfQuestions} questions.");

        prompt.AppendLine(
            "Difficulty must be beginner, intermediate, or advanced.");

        prompt.AppendLine(
            "Category must be technical, behavioural, situational, company, or general.");

        prompt.AppendLine(
            "Keep each interviewer expectation under 50 words.");

        prompt.AppendLine(
            "Return valid JSON only.");

        return prompt.ToString();
    }

    private static string BuildFeedbackPrompt(
        InterviewAnswerRequest request)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(
            "You are a fair and constructive interview coach.");

        prompt.AppendLine(
            "Evaluate the candidate's answer based only on the supplied question, answer, role, and job description.");

        prompt.AppendLine(
            "Do not penalise accent, dialect, or writing style unless meaning is unclear.");

        prompt.AppendLine(
            "For behavioural questions, consider the STAR method: situation, task, action, and result.");

        prompt.AppendLine();
        prompt.AppendLine("ROLE");
        prompt.AppendLine(
            $"Job title: {request.JobTitle}");

        prompt.AppendLine(
            $"Company: {Fallback(request.CompanyName)}");

        prompt.AppendLine(
            $"Job description: {Fallback(request.JobDescription)}");

        prompt.AppendLine();
        prompt.AppendLine("INTERVIEW QUESTION");
        prompt.AppendLine(request.Question);

        prompt.AppendLine();
        prompt.AppendLine("CANDIDATE ANSWER");
        prompt.AppendLine(request.CandidateAnswer);

        prompt.AppendLine();
        prompt.AppendLine("INSTRUCTIONS");

        prompt.AppendLine(
            "Give a score from 0 to 100.");

        prompt.AppendLine(
            "Return no more than four strengths.");

        prompt.AppendLine(
            "Return no more than four improvements.");

        prompt.AppendLine(
            "Provide a stronger example answer without inventing qualifications or experience.");

        prompt.AppendLine(
            "Keep the improved answer under 180 words.");

        prompt.AppendLine(
            "Return valid JSON only.");

        return prompt.ToString();
    }

    private T ParseJson<T>(
        string rawJson,
        string operationName)
    {
        try
        {
            string cleaned = ExtractJson(rawJson);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            T? result =
                JsonSerializer.Deserialize<T>(
                    cleaned,
                    options);

            return result
                ?? throw new InvalidOperationException(
                    $"Gemini returned an empty {operationName} result.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Unable to parse {OperationName}: {RawJson}",
                operationName,
                rawJson);

            throw new InvalidOperationException(
                $"The AI returned invalid {operationName}.",
                ex);
        }
    }

    private static string ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException(
                "The AI response was empty.");
        }

        string cleaned = raw.Trim();

        if (cleaned.StartsWith("```"))
        {
            int firstNewLine = cleaned.IndexOf('\n');

            if (firstNewLine >= 0)
            {
                cleaned = cleaned[(firstNewLine + 1)..];
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

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Not provided"
            : value.Trim();
    }

    private static object CreateQuestionSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                jobTitle = new
                {
                    type = "string"
                },
                companyName = new
                {
                    type = "string"
                },
                questions = new
                {
                    type = "array",
                    minItems = 1,
                    maxItems = 10,
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            number = new
                            {
                                type = "integer"
                            },
                            question = new
                            {
                                type = "string"
                            },
                            category = new
                            {
                                type = "string",
                                @enum = new[]
                                {
                                    "technical",
                                    "behavioural",
                                    "situational",
                                    "company",
                                    "general"
                                }
                            },
                            difficulty = new
                            {
                                type = "string",
                                @enum = new[]
                                {
                                    "beginner",
                                    "intermediate",
                                    "advanced"
                                }
                            },
                            whatInterviewerLooksFor = new
                            {
                                type = "string"
                            }
                        },
                        required = new[]
                        {
                            "number",
                            "question",
                            "category",
                            "difficulty",
                            "whatInterviewerLooksFor"
                        }
                    }
                }
            },
            required = new[]
            {
                "jobTitle",
                "companyName",
                "questions"
            }
        };
    }

    private static object CreateFeedbackSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                score = new
                {
                    type = "integer",
                    minimum = 0,
                    maximum = 100
                },
                overallAssessment = new
                {
                    type = "string"
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
                improvements = new
                {
                    type = "array",
                    maxItems = 4,
                    items = new
                    {
                        type = "string"
                    }
                },
                improvedAnswer = new
                {
                    type = "string"
                },
                deliveryTip = new
                {
                    type = "string"
                }
            },
            required = new[]
            {
                "score",
                "overallAssessment",
                "strengths",
                "improvements",
                "improvedAnswer",
                "deliveryTip"
            }
        };
    }

    public async Task<VoiceInterviewResponse> EvaluateVoiceAnswerAsync(
    string jobTitle,
    string companyName,
    string jobDescription,
    string question,
    IFormFile audioFile,
    CancellationToken cancellationToken = default)
    {
        if (audioFile is null || audioFile.Length == 0)
        {
            throw new ArgumentException(
                "An audio file is required.");
        }

        const long maximumAudioSize =
            10 * 1024 * 1024;

        if (audioFile.Length > maximumAudioSize)
        {
            throw new ArgumentException(
                "The audio file must be 10 MB or smaller.");
        }

        string contentType =
            string.IsNullOrWhiteSpace(
                audioFile.ContentType)
                ? "audio/webm"
                : audioFile.ContentType;

        string extension =
        Path.GetExtension(audioFile.FileName)
        .ToLowerInvariant();

        if (extension == ".m4a" &&
            string.Equals(
                contentType,
                "application/octet-stream",
                StringComparison.OrdinalIgnoreCase))
        {
            contentType = "audio/mp4";
        }

        string[] allowedContentTypes =
        [
            "audio/webm",
            "audio/wav",
            "audio/x-wav",
            "audio/mpeg",
            "audio/mp3",
            "audio/mp4",
            "audio/m4a",
            "audio/aac",
            "audio/ogg",
            "audio/flac",
            "audio/aiff",
            "application/octet-stream"
        ];

        if (!allowedContentTypes.Contains(
            contentType,
            StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Unsupported audio format: {contentType}");
        }

        await using var memoryStream =
            new MemoryStream();

        await audioFile.CopyToAsync(
            memoryStream,
            cancellationToken);

        string transcription =
            await _geminiService.TranscribeAudioAsync(
                memoryStream.ToArray(),
                contentType,
                cancellationToken);

        var evaluationRequest =
            new InterviewAnswerRequest
            {
                JobTitle = jobTitle,
                CompanyName = companyName,
                JobDescription = jobDescription,
                Question = question,
                CandidateAnswer = transcription
            };

        InterviewFeedbackResponse feedback =
            await EvaluateAnswerAsync(
                evaluationRequest,
                cancellationToken);

        return new VoiceInterviewResponse
        {
            Transcription = transcription,
            Feedback = feedback
        };
    }
}