using System.Text;
using System.Text.Json;
using Dwuma.Models.Interview;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Dwuma.Extensions;

namespace Dwuma.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ai-policy")]
[Route("api/interview")]
public sealed class InterviewCoachController : ControllerBase
{
    private readonly InterviewCoachService _interviewCoach;
    private readonly ILogger<InterviewCoachController> _logger;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient ExternalHttpClient = new();

    public InterviewCoachController(
        InterviewCoachService interviewCoach,
        ILogger<InterviewCoachController> logger,
        IConfiguration configuration)
    {
        _interviewCoach = interviewCoach;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("simli/session-token")]
    [DisableRateLimiting]
    public async Task<IActionResult> CreateSimliSessionToken(
        CancellationToken cancellationToken)
    {
        string? apiKey =
            _configuration["Simli:ApiKey"] ??
            _configuration["SIMLI_API_KEY"];

        string? faceId =
            _configuration["Simli:FaceId"] ??
            _configuration["SIMLI_FACE_ID"];

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(faceId))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message =
                        "The live interviewer is not configured yet."
                });
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.simli.ai/compose/token");

        request.Headers.Add("x-simli-api-key", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                faceId,
                apiVersion = "v2",
                handleSilence = true,
                maxSessionLength = 900,
                maxIdleTime = 180,
                startFrame = 0,
                audioInputFormat = "pcm16"
            }),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await ExternalHttpClient.SendAsync(
                request,
                cancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Simli session creation failed with status {StatusCode}: {Body}",
                response.StatusCode,
                body);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The live interviewer could not connect."
                });
        }

        using JsonDocument json = JsonDocument.Parse(body);
        string? sessionToken = json.RootElement
            .GetProperty("session_token")
            .GetString();

        return Ok(new { sessionToken });
    }

    [HttpPost("speech")]
    [DisableRateLimiting]
    public async Task<IActionResult> GenerateInterviewerSpeech(
        [FromBody] InterviewerSpeechRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new
            {
                message = "Speech text is required."
            });
        }

        string? geminiApiKey =
            _configuration["Gemini:ApiKey"] ??
            _configuration["GEMINI_API_KEY"];

        if (string.IsNullOrWhiteSpace(geminiApiKey))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message =
                        "The interviewer voice is not configured yet."
                });
        }

        using var geminiRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1beta/interactions");

        geminiRequest.Headers.Add("x-goog-api-key", geminiApiKey);
        geminiRequest.Headers.Add("Api-Revision", "2026-05-20");
        geminiRequest.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = "gemini-3.1-flash-tts-preview",
                input =
                    $"Speak as a calm, warm and professional job interviewer. " +
                    $"Use a natural conversational pace and read the following exactly: {request.Text.Trim()}",
                response_format = new
                {
                    type = "audio"
                },
                generation_config = new
                {
                    speech_config = new[]
                    {
                        new { voice = "Kore" }
                    }
                }
            }),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage geminiResponse =
            await ExternalHttpClient.SendAsync(
                geminiRequest,
                cancellationToken);

        string geminiBody =
            await geminiResponse.Content.ReadAsStringAsync(
                cancellationToken);

        if (!geminiResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Gemini TTS failed with status {StatusCode}: {Body}",
                geminiResponse.StatusCode,
                geminiBody);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The interviewer voice could not be generated."
                });
        }

        using JsonDocument json = JsonDocument.Parse(geminiBody);

        if (!json.RootElement.TryGetProperty("output_audio", out JsonElement outputAudio) ||
            !outputAudio.TryGetProperty("data", out JsonElement dataElement))
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The interviewer voice response did not contain audio."
                });
        }

        byte[] pcmAudio = Convert.FromBase64String(
            dataElement.GetString() ?? string.Empty);

        Response.Headers["X-Audio-Sample-Rate"] = "24000";
        Response.Headers["Cache-Control"] = "no-store";

        return File(
            pcmAudio,
            "application/octet-stream",
            enableRangeProcessing: false);
    }

    [HttpPost("questions")]
    [ProducesResponseType(
        typeof(InterviewQuestionResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateQuestions(
        [FromBody] InterviewQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            InterviewQuestionResponse response =
                await _interviewCoach.GenerateQuestionsAsync(
                    request,
                    User.GetUserId(),
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Interview question generation failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The interview service could not generate questions."
                });
        }
    }

    [HttpPost("evaluate")]
    [ProducesResponseType(
        typeof(InterviewFeedbackResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateAnswer(
        [FromBody] InterviewAnswerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            InterviewFeedbackResponse response =
                await _interviewCoach.EvaluateAnswerAsync(
                    request,
                    User.GetUserId(),
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Interview answer evaluation failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The interview service could not evaluate the answer."
                });
        }
    }

    [HttpPost("sessions/{sessionId:int}/complete")]
    public async Task<IActionResult> CompleteInterview(
        int sessionId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _interviewCoach.CompleteInterviewAsync(
                sessionId, User.GetUserId(), cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("latest")]
    [DisableRateLimiting]
    public async Task<IActionResult> Latest(CancellationToken cancellationToken)
    {
        LatestInterviewResponse? result = await _interviewCoach.GetLatestInterviewAsync(
            User.GetUserId(), cancellationToken);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpPost("evaluate-voice")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
    typeof(VoiceInterviewResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> EvaluateVoiceAnswer(
    [FromForm] VoiceInterviewRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            if (request.AudioFile is null)
            {
                return BadRequest(new
                {
                    message = "An audio file is required."
                });
            }

            VoiceInterviewResponse response =
                await _interviewCoach.EvaluateVoiceAnswerAsync(
                    User.GetUserId(),
                    request.SessionId,
                    request.QuestionId,
                    request.JobTitle,
                    request.CompanyName,
                    request.JobDescription,
                    request.Question,
                    request.AudioFile,
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Voice interview evaluation failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The voice interview could not be evaluated."
                });
        }
    }
}
