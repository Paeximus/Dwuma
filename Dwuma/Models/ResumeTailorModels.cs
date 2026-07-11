using System.ComponentModel.DataAnnotations;

namespace Dwuma.Models
{
    // ─── REQUEST ─────────────────────────────────────────────────────────────

    public class TailorRequest
    {
        [Required] public string CvText        { get; set; } = string.Empty;
        [Required] public string JobTitle      { get; set; } = string.Empty;
        [Required] public string JobDescription{ get; set; } = string.Empty;
        public string? CompanyName    { get; set; }
        public string  TailoringFocus { get; set; } = "balanced";
    }

    public class DownloadRequest
    {
        [Required] public string TailoredCv  { get; set; } = string.Empty;
        [Required] public string JobTitle    { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
    }

    // ─── RESPONSE ────────────────────────────────────────────────────────────

    public class TailorResponse
    {
        public string TailoredCv      { get; set; } = string.Empty;
        public int    AtsScore        { get; set; }
        public string AtsSummary      { get; set; } = string.Empty;
        public List<string> MatchedKeywords { get; set; } = new();
        public List<string> MissingKeywords { get; set; } = new();
        public List<ChangelogItem> Changelog { get; set; } = new();
    }

    public class ChangelogItem
    {
        /// <summary>rewrite | added | removed | keyword</summary>
        public string Type    { get; set; } = "rewrite";
        public string Section { get; set; } = string.Empty;
        public string Reason  { get; set; } = string.Empty;
    }

    // ─── CV VERSION (stored in DB) ────────────────────────────────────────────

    public class CvVersion
    {
        public int    Id          { get; set; }
        public int    UserId      { get; set; }
        public string JobTitle    { get; set; } = string.Empty;
        public string? CompanyName{ get; set; }
        public string TailoredCv  { get; set; } = string.Empty;
        public int    AtsScore    { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
