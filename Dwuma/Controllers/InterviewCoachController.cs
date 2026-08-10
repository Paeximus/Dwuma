using Dwuma.Extensions;
using Dwuma.Models.Interview;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO;

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

        try
        {
            var modelPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Piper",
                "voices",
                "en_US-ryan-medium.onnx"
            );

            if (!System.IO.File.Exists(modelPath))
            {
                _logger.LogError(
                    "Piper voice model was not found at {ModelPath}",
                    modelPath
                );

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message = "The interviewer voice model could not be found."
                    }
                );
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "piper",
                Arguments =
                    $"-m \"{modelPath}\" --output-raw",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            await process.StandardInput.WriteLineAsync(
                request.Text.Trim()
            );

            process.StandardInput.Close();

            using var audioStream = new MemoryStream();

            await process.StandardOutput.BaseStream.CopyToAsync(
                audioStream,
                cancellationToken
            );

            string errorOutput =
                await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(
                cancellationToken
            );

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "Piper TTS failed with exit code {ExitCode}: {Error}",
                    process.ExitCode,
                    errorOutput
                );

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        message = "The interviewer voice could not be generated."
                    }
                );
            }

            byte[] pcmAudio =
                audioStream.ToArray();

            if (pcmAudio.Length == 0)
            {
                _logger.LogError(
                    "Piper returned an empty audio response."
                );

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        message = "The interviewer voice contained no audio."
                    }
                );
            }

            Response.Headers["X-Audio-Sample-Rate"] = "22050";
            Response.Headers["Cache-Control"] = "no-store";

            return File(
                pcmAudio,
                "application/octet-stream",
                enableRangeProcessing: false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Piper failed while generating interviewer speech."
            );

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "The interviewer voice could not be generated."
                }
            );
        }
    }

    [HttpPost("video-session")]
    [DisableRateLimiting]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> UploadVideoSession(
    [FromForm] InterviewVideoSessionRequest request,
    CancellationToken cancellationToken)
    {
        if (request.VideoFile == null ||
            request.VideoFile.Length == 0)
        {
            return BadRequest(new
            {
                message = "Interview video is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(new
            {
                message = "Interview session ID is required."
            });
        }

        try
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                "Interviews"
            );

            Directory.CreateDirectory(uploadsFolder);

            var fileName =
                $"{request.SessionId}_{Guid.NewGuid()}.webm";

            var filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName
                );

            await using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    )
            )
            {
                await request.VideoFile.CopyToAsync(
                    stream,
                    cancellationToken
                );
            }

            _logger.LogInformation(
                "Interview video uploaded. Session: {SessionId}, File: {FileName}",
                request.SessionId,
                fileName
            );

            return Ok(new
            {
                message =
                    "Interview video uploaded successfully.",

                sessionId =
                    request.SessionId,

                fileName,

                videoSize =
                    request.VideoFile.Length,

                questionTimingsJson =
                    request.QuestionTimingsJson
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unable to upload interview video for session {SessionId}",
                request.SessionId
            );

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "The interview video could not be uploaded."
                }
            );
        }
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
