using Microsoft.AspNetCore.Http;

namespace Dwuma.Models.Interview;

public sealed class TranscribeAnswerRequest
{
    public IFormFile AudioFile { get; set; } = null!;
}