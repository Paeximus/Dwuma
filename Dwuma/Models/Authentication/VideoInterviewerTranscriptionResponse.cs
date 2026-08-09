namespace Dwuma.Models.Interview;

public sealed class VideoInterviewTranscriptionResponse
{
    public List<VideoInterviewAnswerTranscript> Answers { get; set; }
        = [];
}

public sealed class VideoInterviewAnswerTranscript
{
    public int QuestionId { get; set; }

    public int QuestionNumber { get; set; }

    public string Question { get; set; }
        = string.Empty;

    public string Transcript { get; set; }
        = string.Empty;

    public double AnswerStartedAt { get; set; }

    public double AnswerEndedAt { get; set; }
}