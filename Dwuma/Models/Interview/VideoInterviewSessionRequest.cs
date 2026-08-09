using Microsoft.AspNetCore.Http;

namespace Dwuma.Models.Interview;

public sealed class VideoInterviewSessionRequest
{
    public int SessionId { get; set; }

    public string JobTitle { get; set; }
        = string.Empty;

    public string CompanyName { get; set; }
        = string.Empty;

    public string JobDescription { get; set; }
        = string.Empty;

    public IFormFile? VideoFile { get; set; }

    public string QuestionTimingsJson { get; set; }
        = string.Empty;
}