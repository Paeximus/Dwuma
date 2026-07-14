namespace Dwuma.Models.Interview;

public sealed class VoiceInterviewResponse
{
    public string Transcription { get; set; } = string.Empty;

    public InterviewFeedbackResponse Feedback { get; set; } = new();
}