namespace Dwuma.Models.Interview;

public sealed class VideoQuestionTiming
{
    public int QuestionId { get; set; }

    public int QuestionNumber { get; set; }

    public string Question { get; set; }
        = string.Empty;

    public double QuestionStartedAt { get; set; }

    public double AnswerStartedAt { get; set; }

    public double AnswerEndedAt { get; set; }

    public double Duration { get; set; }
}