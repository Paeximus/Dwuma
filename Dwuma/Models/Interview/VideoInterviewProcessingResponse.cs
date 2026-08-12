namespace Dwuma.Models.Interview
{
    public sealed class VideoInterviewProcessingResponse
    {
        public List<VideoInterviewAnswerAnalysis> Answers { get; set; } =
            [];

        public string OverallVideoFeedback { get; set; } =
            string.Empty;
    }
}
