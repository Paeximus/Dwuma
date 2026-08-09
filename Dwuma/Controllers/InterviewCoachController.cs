using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Interview;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;


namespace Dwuma.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ai-policy")]
[Route("api/interview")]
public sealed class InterviewCoachController : ControllerBase
{
    private readonly InterviewCoachService _interviewCoach;
    private readonly ILogger<InterviewCoachController> _logger;
    private readonly NotificationService _notificationService;
    private readonly DwumaContext _context;

    public InterviewCoachController(
        InterviewCoachService interviewCoach,
        ILogger<InterviewCoachController> logger,
        NotificationService notificationService,
        DwumaContext context)
    {
        _interviewCoach = interviewCoach;
        _logger = logger;
        _notificationService = notificationService;
        _context = context;
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
                await _interviewCoach
                    .GenerateQuestionsAsync(
                        request,
                        cancellationToken);

            int userId =
                GetUserId();

            var session =
                new InterviewSession
                {
                    UserId =
                        userId,

                    JobListingId =
                        null,

                    Company =
                        request.CompanyName,

                    Role =
                        request.JobTitle,

                    StartedAt =
                        DateTime.UtcNow,

                    CompletedAt =
                        null
                };

            _context.InterviewSessions.Add(
                session);

            await _context.SaveChangesAsync(
                cancellationToken);

            response.SessionId =
                session.Id;

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
                    cancellationToken);


            int userId = GetUserId();

            await _notificationService.CreatePersonalizedAsync(
                    userId,
                    "Interview",
                    $"you scored {response.Score}% in your {request.JobTitle} interview practice. Review your feedback to improve your next attempt.",
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


    [HttpPost("video-session")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> UploadVideoSession(
    [FromForm] VideoInterviewSessionRequest request,
    CancellationToken cancellationToken)
    {
        if (request.VideoFile == null ||
            request.VideoFile.Length == 0)
        {
            return BadRequest(new
            {
                message =
                    "Interview video is required."
            });
        }

        if (request.SessionId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid interview session ID is required."
            });
        }

        List<VideoQuestionTiming>? timings;

        try
        {
            timings =
                JsonSerializer.Deserialize<
                    List<VideoQuestionTiming>>(
                    request.QuestionTimingsJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                message =
                    "Question timing data is invalid."
            });
        }

        timings ??= [];

        if (timings.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "Question timing data is required."
            });
        }

        string contentType =
            request.VideoFile.ContentType
                ?.ToLowerInvariant()
            ?? string.Empty;

        string extension =
            Path.GetExtension(
                request.VideoFile.FileName)
            .ToLowerInvariant();

        bool supportedVideo =
            contentType.StartsWith(
                "video/webm") ||
            contentType.StartsWith(
                "video/mp4") ||
            extension == ".webm" ||
            extension == ".mp4";

        if (!supportedVideo)
        {
            return BadRequest(new
            {
                message =
                    "Only WebM or MP4 interview recordings are supported."
            });
        }

        // Verify that this session belongs
        // to the authenticated user.
        int userId = GetUserId();

        var session =
            await _context.InterviewSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s =>
                        s.Id ==
                            request.SessionId &&
                        s.UserId ==
                            userId,
                    cancellationToken);

        if (session == null)
        {
            return NotFound(new
            {
                message =
                    "Interview session was not found."
            });
        }



        return Ok(new
        {
            message =
        "Interview video uploaded successfully.",

            sessionId =
        request.SessionId,

            fileName =
        request.VideoFile.FileName,

            contentType =
        request.VideoFile.ContentType,

            fileSize =
        request.VideoFile.Length,

            questionCount =
        timings.Count
        });

        }

    [HttpPost("sessions/{sessionId:int}/complete")]
    public async Task<IActionResult> CompleteInterview(
    int sessionId,
    CancellationToken cancellationToken)
    {
        int userId = GetUserId();

        var session =
            await _context.InterviewSessions
                .FirstOrDefaultAsync(
                    s =>
                        s.Id == sessionId &&
                        s.UserId == userId,
                    cancellationToken);

        if (session == null)
        {
            return NotFound(new
            {
                message =
                    "Interview session was not found."
            });
        }

        if (session.CompletedAt == null)
        {
            session.CompletedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        return Ok(new
        {
            sessionId =
                session.Id,

            completed =
                true,

            completedAt =
                session.CompletedAt,

            role =
                session.Role,

            company =
                session.Company
        });
    }

    [HttpPost("transcribe-answer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> TranscribeAnswer(
    [FromForm] IFormFile audioFile,
    CancellationToken cancellationToken)
    {
        if (audioFile == null ||
            audioFile.Length == 0)
        {
            return BadRequest(new
            {
                message =
                    "An audio recording is required."
            });
        }

        const long maximumAudioSize =
            15 * 1024 * 1024;

        if (audioFile.Length >
            maximumAudioSize)
        {
            return BadRequest(new
            {
                message =
                    "The audio recording is too large."
            });
        }

        string contentType =
            string.IsNullOrWhiteSpace(
                audioFile.ContentType)
                ? "audio/webm"
                : audioFile.ContentType;

        if (contentType.StartsWith(
                "audio/webm",
                StringComparison.OrdinalIgnoreCase))
        {
            contentType =
                "audio/webm";
        }

        await using var memoryStream =
            new MemoryStream();

        await audioFile.CopyToAsync(
            memoryStream,
            cancellationToken);

        try
        {
            string transcript =
                await _interviewCoach
                    .TranscribeAnswerAudioAsync(
                        memoryStream.ToArray(),
                        contentType,
                        cancellationToken);

            return Ok(new
            {
                transcript =
                    transcript.Trim()
            });
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
                "Interview answer transcription failed.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The spoken answer could not be transcribed."
                });
        }
    }

    private int GetUserId()
    {
        string? userIdValue =
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value;

        if (!int.TryParse(
                userIdValue,
                out int userId))
        {
            throw new UnauthorizedAccessException(
                "Unable to identify the current user.");
        }

        return userId;
    }
}