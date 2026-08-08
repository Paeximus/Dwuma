using Dwuma.Models.Interview;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    public InterviewCoachController(
        InterviewCoachService interviewCoach,
        ILogger<InterviewCoachController> logger,
        NotificationService notificationService)
    {
        _interviewCoach = interviewCoach;
        _logger = logger;
        _notificationService = notificationService;
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
                    cancellationToken);


            int userId = GetUserId();

            await _notificationService.CreateAsync(
                userId,
                "Your interview feedback and score are ready to review.",
                "Interview",
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