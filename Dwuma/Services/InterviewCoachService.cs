using Dwuma.Models.Interview;
using Dwuma.Scraping.Services;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Dwuma.Services;

public sealed class InterviewCoachService
{
    private readonly GeminiService _geminiService;
    private readonly ILogger<InterviewCoachService> _logger;
    private readonly NotificationService _notificationService;
    private readonly InterviewQuestionScraper _scraper;

    public InterviewCoachService(
        GeminiService geminiService,
        ILogger<InterviewCoachService> logger,
        NotificationService notificationService,
        InterviewQuestionScraper scraper)
    {
        _geminiService = geminiService;
        _logger = logger;
        _notificationService = notificationService;
        _scraper = scraper;
    }

    public async Task<InterviewQuestionResponse> GenerateQuestionsAsync(
    InterviewQuestionRequest request,
    CancellationToken cancellationToken = default)
    {
        ValidateQuestionRequest(request);

        if (!IsAllowedInterviewRole(
                request.JobTitle,
                request.JobDescription))
        {
            return new InterviewQuestionResponse
            {
                JobTitle =
                    request.JobTitle,

                CompanyName =
                    request.CompanyName ?? string.Empty,

                Notice =
                    "Interview preparation is not available for illegal or harmful job roles.",

                Questions = []
            };
        }

        request.NumberOfQuestions =
            Math.Clamp(
                request.NumberOfQuestions,
                1,
                10);

        // Gemini still generates the full interview.
        // We will replace Q1, Q2 and Q3 afterwards.
        string prompt =
            BuildQuestionPrompt(request);

        string rawJson =
            await _geminiService.GenerateJsonAsync(
                prompt,
                responseSchema:
                    CreateQuestionSchema(),
                maxOutputTokens: 4000,
                cancellationToken:
                    cancellationToken);

        InterviewQuestionResponse response =
            ParseJson<InterviewQuestionResponse>(
                rawJson,
                "interview questions");

        response.Questions ??= [];

        // ------------------------------------------------
        // Q1 AND Q2 = GENERIC
        // Q3 = SCRAPED
        // Q4+ = KEEP GEMINI QUESTIONS
        // ------------------------------------------------

        List<string> fixedQuestions =
            await BuildInterviewQuestionsAsync(
                request.JobTitle,
                request.NumberOfQuestions,
                cancellationToken);

        // Q1
        if (
            response.Questions.Count >= 1 &&
            fixedQuestions.Count >= 1)
        {
            response.Questions[0].Question =
                fixedQuestions[0];

            response.Questions[0].Number = 1;

            response.Questions[0].Category =
                "general";

            response.Questions[0].Difficulty =
                "beginner";

            response.Questions[0]
                .WhatInterviewerLooksFor =
                "A concise professional introduction covering relevant background, skills, experience, and career interests.";
        }

        // Q2
        if (
            response.Questions.Count >= 2 &&
            fixedQuestions.Count >= 2)
        {
            response.Questions[1].Question =
                fixedQuestions[1];

            response.Questions[1].Number = 2;

            response.Questions[1].Category =
                "general";

            response.Questions[1].Difficulty =
                "beginner";

            response.Questions[1]
                .WhatInterviewerLooksFor =
                "Clear motivation for the role and an understanding of how it connects with the candidate's skills and career goals.";
        }

        // Q3
        if (
    response.Questions.Count >= 3 &&
    fixedQuestions.Count >= 3)
        {
            // Replace Gemini Q3 only when
            // scraping actually returned a question.
            response.Questions[2].Question =
                fixedQuestions[2];

            response.Questions[2].Number = 3;

            response.Questions[2].Category =
                "technical";

            response.Questions[2].Difficulty =
                "intermediate";

            response.Questions[2]
                .WhatInterviewerLooksFor =
                "A clear, relevant and structured response demonstrating practical understanding of the role.";
        }

        // Make sure numbering remains correct
        for (
            int index = 0;
            index < response.Questions.Count;
            index++)
        {
            response.Questions[index].Number =
                index + 1;
        }

        return response;
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

        if (string.IsNullOrWhiteSpace(
                request.JobTitle))
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
        ValidateInterviewSafety(request.JobTitle, request.JobDescription);
    }

    private static void ValidateInterviewSafety(
    string? jobTitle,
    string? jobDescription = null)
    {
        string combined =
            $"{jobTitle} {jobDescription}"
                .Trim()
                .ToLowerInvariant();

        string[] prohibitedTerms =
        [
            // Illegal drug production / trafficking
            "meth production",
        "meth producer",
        "meth manufacturer",
        "meth lab",
        "cook meth",
        "cocaine production",
        "cocaine manufacturer",
        "drug trafficking",
        "drug trafficker",
        "illegal drug manufacturing",

        // Violence / murder-for-hire
        "hitman",
        "contract killer",
        "assassin for hire",
        "murder for hire",

        // Human trafficking
        "human trafficker",
        "human trafficking",
        "sex trafficking",

        // Illegal weapons / explosives
        "bomb maker",
        "bomb making",
        "illegal arms dealer",
        "illegal weapons dealer",

        // Fraud / theft
        "credit card fraud",
        "identity theft",
        "fraud operator",
        "scam operator",

        // Malicious cybercrime
        "ransomware operator",
        "malware for theft",
        "phishing scammer",
        "credential thief"
        ];

        bool prohibited =
            prohibitedTerms.Any(term =>
                combined.Contains(
                    term,
                    StringComparison.OrdinalIgnoreCase));

        if (prohibited)
        {
            throw new ArgumentException(
                "Interview preparation is not available for illegal or harmful job roles.");
        }
    }

    private static string BuildQuestionPrompt(
        InterviewQuestionRequest request)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(
            "You are a professional interview coach.");

        prompt.AppendLine(
    "Only provide interview preparation for legitimate and lawful employment.");

        prompt.AppendLine(
            "Do not generate interview questions, explanations, instructions, procedures, or advice that would facilitate criminal activity, illegal drug production, trafficking, violence, fraud, theft, malicious hacking, illegal weapons activity, or exploitation.");

        prompt.AppendLine(
            "If a role is disguised but clearly involves illegal or harmful activity, do not provide operational guidance.");

        prompt.AppendLine(
            "Legitimate sensitive professions such as cybersecurity, chemistry, medicine, law enforcement, forensics, and regulated engineering are allowed, but questions must remain lawful, defensive, safety-focused, and professional.");

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
            "Only evaluate answers for legitimate and lawful professional activity.");

        prompt.AppendLine(
            "Do not improve, optimize, correct, or expand an answer in a way that provides instructions or practical guidance for criminal or harmful activity.");

        prompt.AppendLine(
            "This includes illegal drug production or trafficking, violence, fraud, theft, malicious hacking, human trafficking, illegal weapons activity, or other criminal conduct.");

        prompt.AppendLine(
            "For legitimate sensitive professions such as cybersecurity, chemistry, medicine, law enforcement, forensics, and engineering, keep feedback lawful, defensive, ethical, safety-focused, and compliance-focused.");

        prompt.AppendLine(
            "If the supplied role, question, or answer clearly requests harmful or illegal operational guidance, do not provide an improved operational answer.");

        prompt.AppendLine(
            "Evaluate the candidate's answer based only on the supplied question, answer, role, and job description.");

        prompt.AppendLine(
            "Do not penalise accent, dialect, or writing style unless meaning is unclear.");

        prompt.AppendLine(
            "For behavioural questions, consider the STAR method: situation, task, action, and result.");

        // KEEP THE REST OF YOUR EXISTING METHOD HERE
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
            "For legitimate interview content, provide a stronger example answer without inventing qualifications or experience.");

        prompt.AppendLine(
            "If the content involves illegal or harmful operational activity, do not provide an improved operational answer; instead give a brief safety-focused response.");

        prompt.AppendLine(
            "Keep the improved answer under 180 words.");

        prompt.AppendLine(
            "Return valid JSON only.");

        return prompt.ToString();
    }

    private static bool IsAllowedInterviewRole(
    string? jobTitle,
    string? jobDescription = null)
    {
        string combined =
            $"{jobTitle} {jobDescription}"
                .Trim()
                .ToLowerInvariant();

        string[] prohibitedTerms =
        [
            "meth production",
        "meth producer",
        "meth manufacturer",
        "meth lab",
        "cook meth",
        "cocaine production",
        "cocaine manufacturer",
        "drug trafficking",
        "drug trafficker",
        "illegal drug manufacturing",

        "hitman",
        "contract killer",
        "assassin for hire",
        "murder for hire",

        "human trafficker",
        "human trafficking",
        "sex trafficking",

        "bomb maker",
        "bomb making",
        "illegal arms dealer",
        "illegal weapons dealer",

        "credit card fraud",
        "identity theft",
        "fraud operator",
        "scam operator",

        "ransomware operator",
        "malware for theft",
        "phishing scammer",
        "credential thief"
        ];

        return !prohibitedTerms.Any(term =>
            combined.Contains(
                term,
                StringComparison.OrdinalIgnoreCase));
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

    private static readonly string[] GenericInterviewQuestions =
    {
        "Tell me about yourself.",
        "Why are you interested in this role?"
    };

    private async Task<List<string>> BuildInterviewQuestionsAsync(
    string role,
    int totalQuestions,
    CancellationToken cancellationToken)
    {
        var finalQuestions = new List<string>();

        // Q1 and Q2 are always generic
        finalQuestions.AddRange(
            GenericInterviewQuestions);

        if (totalQuestions <= 2)
        {
            return finalQuestions
                .Take(totalQuestions)
                .ToList();
        }

        try
        {
            var scrapedQuestions =
                await _scraper.ScrapeAsync(
                    "https://www.indeed.com/career-advice/interviewing/behavioral-interview-questions",
                    "Indeed",
                    role,
                    cancellationToken);
            var allQuestions =
                scrapedQuestions
                    .Where(q =>
                        !string.IsNullOrWhiteSpace(
                            q.Question))
                    .Select(q =>
                        q.Question.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            var roleRelevantQuestions =
                allQuestions
                    .Where(q =>
                        MatchesRole(
                            q,
                            role))
                    .ToList();

            var questionPool =
                roleRelevantQuestions.Count > 0
                    ? roleRelevantQuestions
                    : allQuestions;

            string? scrapedQuestion =
                questionPool
                    .OrderBy(_ =>
                        Guid.NewGuid())
                    .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(
                scrapedQuestion))
            {
                finalQuestions.Add(
                    scrapedQuestion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to retrieve scraped interview question for role {Role}. Gemini question will be used as fallback.",
                role);
        }

        return finalQuestions
            .Take(totalQuestions)
            .ToList();
    }

    private static bool MatchesRole(
    string question,
    string? role)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return true;
        }

        string normalizedQuestion =
            question.ToLowerInvariant();

        string[] roleWords =
            role
                .ToLowerInvariant()
                .Split(
                    ' ',
                    StringSplitOptions
                        .RemoveEmptyEntries);

        return roleWords.Any(
            word =>
                word.Length >= 3 &&
                normalizedQuestion.Contains(
                    word,
                    StringComparison.OrdinalIgnoreCase));
    }

    public async Task<VideoInterviewTranscriptionResponse>
    TranscribeVideoInterviewAsync(
        IFormFile videoFile,
        List<VideoQuestionTiming> timings,
        CancellationToken cancellationToken = default)
    {
        if (videoFile is null ||
            videoFile.Length == 0)
        {
            throw new ArgumentException(
                "Interview video is required.");
        }

        if (timings is null ||
            timings.Count == 0)
        {
            throw new ArgumentException(
                "Question timing data is required.");
        }

        string contentType =
    string.IsNullOrWhiteSpace(
        videoFile.ContentType)
        ? "video/webm"
        : videoFile.ContentType;

        if (contentType.StartsWith(
                "video/webm",
                StringComparison.OrdinalIgnoreCase))
        {
            contentType =
                "video/webm";
        }
        else if (contentType.StartsWith(
                     "video/mp4",
                     StringComparison.OrdinalIgnoreCase))
        {
            contentType =
                "video/mp4";
        }
        else
        {
            throw new ArgumentException(
                $"Unsupported interview video format: {contentType}");
        }
        await using var memoryStream =
            new MemoryStream();

        await videoFile.CopyToAsync(
            memoryStream,
            cancellationToken);

        string timingText =
            string.Join(
                Environment.NewLine,
                timings.Select(t =>
                {
                    string start =
                        ToGeminiTimestamp(
                            t.AnswerStartedAt);

                    string end =
                        ToGeminiTimestamp(
                            t.AnswerEndedAt);

                    return
                        $"""
                        Question {t.QuestionNumber}
                        Question: {t.Question}
                        Candidate answer time: {start} to {end}
                        """;
                }));

        string prompt =
            $$"""
            Generate an accurate transcript of the candidate's
            spoken answers in this job interview video.

            The video contains both visual content and an audio track.

            There are exactly {{timings.Count}} interview questions.

            The candidate's answer windows are:

            {{timingText}}

            Instructions:

            - Listen to the AUDIO TRACK of the video.
            - Transcribe the candidate's speech.
            - Use the timestamps above to identify each answer.
            - Ignore the interviewer's spoken questions.
            - Preserve the candidate's actual words.
            - Do not summarize.
            - Do not improve grammar.
            - Do not invent words.
            - Return one answer object for every question.
            - Only return an empty transcript if there is genuinely
              no audible candidate speech during that answer window.

            There must be exactly {{timings.Count}} objects
            in the answers array.
            """;

        string rawJson =
            await _geminiService
                .AnalyzeVideoAsync(
                    memoryStream.ToArray(),
                    contentType,
                    prompt,
                    cancellationToken);
        _logger.LogInformation(
            "Gemini raw video transcription response: {RawJson}",
            rawJson);

        VideoInterviewTranscriptionResponse response =
            ParseJson<VideoInterviewTranscriptionResponse>(
                rawJson,
                "video interview transcription");

        if (response.Answers == null ||
            response.Answers.Count == 0)
        {
            throw new InvalidOperationException(
                "Gemini returned no interview transcripts.");
        }

        foreach (var answer in response.Answers)
        {
            _logger.LogInformation(
                "Transcript Q{QuestionNumber}: {Transcript}",
                answer.QuestionNumber,
                answer.Transcript);
        }

        return response;
    }


    public async Task<string>
    TranscribeAnswerAudioAsync(
        byte[] audioBytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (audioBytes == null ||
            audioBytes.Length == 0)
        {
            throw new ArgumentException(
                "Audio data is required.");
        }

        string normalizedContentType =
            contentType.StartsWith(
                "audio/webm",
                StringComparison.OrdinalIgnoreCase)
                ? "audio/webm"
                : contentType;

        string transcript =
            await _geminiService
                .TranscribeAudioAsync(
                    audioBytes,
                    normalizedContentType,
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
                transcript))
        {
            throw new InvalidOperationException(
                "No speech was detected in the interview answer.");
        }

        return transcript.Trim();
    }

    private static string ToGeminiTimestamp(
    double seconds)
    {
        if (seconds < 0)
        {
            seconds = 0;
        }

        int totalSeconds =
            (int)Math.Floor(seconds);

        int minutes =
            totalSeconds / 60;

        int remainingSeconds =
            totalSeconds % 60;

        return $"{minutes:00}:{remainingSeconds:00}";
    }
}