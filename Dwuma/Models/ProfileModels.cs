using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dwuma.Models
{
    // ─── DATABASE ENTITIES ────────────────────────────────────────────────────

    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        // Personal
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName  { get; set; } = string.Empty;
        [Required] public string Email     { get; set; } = string.Empty;
        public string? Phone    { get; set; }
        public string? Location { get; set; }
        public string? LinkedIn { get; set; }

        // Education
        public string? Institution    { get; set; }
        public string? DegreeLevel    { get; set; }
        public string? FieldOfStudy   { get; set; }
        public string? GraduationYear { get; set; }
        public string? Classification { get; set; }

        // Skills — stored as comma-separated strings for simplicity
        // (normalise into separate tables for production)
        public string? TechSkills     { get; set; }
        public string? SoftSkills     { get; set; }
        public string? Languages      { get; set; }
        public string? Certifications { get; set; }
        public string? YearsExp       { get; set; }

        // Career preferences
        public string? Industries   { get; set; }
        public string? JobTypes     { get; set; }
        public string? WorkLocation { get; set; }
        public string? SalaryRange  { get; set; }
        public string? CareerGoal   { get; set; }

        // CV
        public string? CvFileName   { get; set; }
        public string? CvFilePath   { get; set; }
        public string? CvParsedText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ─── REQUEST / RESPONSE DTOs ──────────────────────────────────────────────

    public class ProfileRequest
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName  { get; set; } = string.Empty;
        [Required] public string Email     { get; set; } = string.Empty;
        public string? Phone        { get; set; }
        public string? Location     { get; set; }
        public string? LinkedIn     { get; set; }
        public string? Institution  { get; set; }
        public string? DegreeLevel  { get; set; }
        public string? FieldOfStudy { get; set; }
        public string? GraduationYear  { get; set; }
        public string? Classification  { get; set; }
        public List<string> Certifications { get; set; } = new();
        public List<string> TechSkills     { get; set; } = new();
        public List<string> SoftSkills     { get; set; } = new();
        public List<string> Languages      { get; set; } = new();
        public string? YearsExp    { get; set; }
        public List<string> Industries { get; set; } = new();
        public List<string> JobTypes   { get; set; } = new();
        public string? WorkLocation { get; set; }
        public string? SalaryRange  { get; set; }
        public string? CareerGoal   { get; set; }
    }

    public class ProfileResponse
    {
        public int    UserId    { get; set; }
        public string Message   { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName  { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
    }
}
