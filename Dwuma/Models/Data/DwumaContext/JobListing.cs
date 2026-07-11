using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class JobListing
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Company { get; set; }

    public string? Location { get; set; }

    public string? JobType { get; set; }

    public string? Description { get; set; }

    public double? RelevanceScore { get; set; }

    public string? Status { get; set; }

    public string? SourceUrl { get; set; }

    public DateTime? DiscoveredAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<CvDocument> CvDocuments { get; set; } = new List<CvDocument>();

    public virtual ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();

    public virtual ICollection<JobInteraction> JobInteractions { get; set; } = new List<JobInteraction>();

    public virtual User User { get; set; } = null!;
}
