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

    public IFormFile VideoFile { get; set; }
        = null!;

    // JSON representation of all
    // question/answer timestamps.
    public string QuestionTimingsJson { get; set; }
        = "[]";
}