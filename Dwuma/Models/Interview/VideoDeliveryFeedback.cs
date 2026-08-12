namespace Dwuma.Models.Interview
{
    public sealed class VideoDeliveryFeedback
    {
        public string CameraPresence { get; set; } = string.Empty;

        public string CameraAttention { get; set; } = string.Empty;

        public string Posture { get; set; } = string.Empty;

        public string Movement { get; set; } = string.Empty;

        public string Visibility { get; set; } = string.Empty;

        public List<string> Strengths { get; set; } = [];

        public List<string> Improvements { get; set; } = [];

        public string DeliveryTip { get; set; } = string.Empty;
    }

}
