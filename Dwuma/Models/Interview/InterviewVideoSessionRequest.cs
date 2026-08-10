using Microsoft.AspNetCore.Http;

namespace Dwuma.Models.Interview;

public sealed class InterviewVideoSessionRequest
{
    public string SessionId { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public string QuestionTimingsJson { get; set; } = string.Empty;

    public IFormFile? VideoFile { get; set; }
}