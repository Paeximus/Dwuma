using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dwuma.Models.Data.DwumaContext;

public partial class JobListing
{
    public int Id { get; set; }

    public string? ExternalId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Company { get; set; }

    public string? Location { get; set; }

    public string? JobType { get; set; }

    public string? Industry { get; set; }

    public string? Description { get; set; }

    // Stored as comma-separated text initially
    public string? RequiredSkills { get; set; }

    public string? Salary { get; set; }

    public bool IsRemote { get; set; }

    // Optional score calculated for a particular search/import
    public double? RelevanceScore { get; set; }

    // Active, Expired, Closed, etc.
    public string? Status { get; set; }

    // Jooble, Careerjet, DWUMA Import, etc. 
    public string? Source { get; set; }

    // External application link
    public string? SourceUrl { get; set; }

    // Date published by the external provider
    public DateTime? PostedAt { get; set; }

    // Date Dwuma fetched the listing
    public DateTime? DiscoveredAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; }
        = new List<Application>();

    public virtual ICollection<CvDocument> CvDocuments { get; set; }
        = new List<CvDocument>();

    public virtual ICollection<InterviewSession> InterviewSessions { get; set; }
        = new List<InterviewSession>();

    public virtual ICollection<JobInteraction> JobInteractions { get; set; }
        = new List<JobInteraction>();
}