namespace Dwuma.Models.Interview
{
    public sealed class VideoInterviewAnswerAnalysis
    {
        public int QuestionId { get; set; }

        public int QuestionNumber { get; set; }

        public string Question { get; set; } = string.Empty;

        public string Transcript { get; set; } = string.Empty;

        public double AnswerStartedAt { get; set; }

        public double AnswerEndedAt { get; set; }

        public VideoDeliveryFeedback VisualFeedback { get; set; } =
            new();
    }
}
