using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? JwtToken { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<CvDocument> CvDocuments { get; set; } = new List<CvDocument>();

    public virtual ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();

    public virtual ICollection<JobInteraction> JobInteractions { get; set; } = new List<JobInteraction>();

    public virtual ICollection<JobListing> JobListings { get; set; } = new List<JobListing>();

    public virtual ICollection<NotificationEngagement> NotificationEngagements { get; set; } = new List<NotificationEngagement>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<SkillGapReport> SkillGapReports { get; set; } = new List<SkillGapReport>();

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
