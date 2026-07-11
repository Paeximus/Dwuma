using System.ComponentModel.DataAnnotations;

namespace Dwuma.Models
{
    // ─── REQUEST ─────────────────────────────────────────────────────────────

    public class SkillsGapRequest
    {
        [Required]
        public List<string> Skills { get; set; } = new();

        public string? Education { get; set; }        // e.g. "BSc"
        public string? FieldOfStudy { get; set; }     // e.g. "Computer Engineering"
        public string? Experience { get; set; }       // e.g. "0", "2"

        [Required]
        public string JobTitle { get; set; } = string.Empty;

        public string? Industry { get; set; }
        public string? JobDescription { get; set; }   // Full JD text, optional
    }

    // ─── RESPONSE ────────────────────────────────────────────────────────────

    public class SkillsGapResponse
    {
        public int MatchPercentage { get; set; }          // 0–100
        public string Summary { get; set; } = string.Empty;
        public List<SkillItem> Skills { get; set; } = new();
        public List<LearningResource> Resources { get; set; } = new();
    }

    public class SkillItem
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>present | partial | absent</summary>
        public string Status { get; set; } = "absent";

        /// <summary>foundational | intermediate | advanced</summary>
        public string Level { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public class LearningResource
    {
        public string Name { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;   // e.g. "Coursera"
        public string Description { get; set; } = string.Empty;
        public string? Url { get; set; }
        public bool IsFree { get; set; }
    }
}
