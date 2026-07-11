using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class InterviewSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? JobListingId { get; set; }

    public string? Company { get; set; }

    public string? Role { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual ICollection<InterviewQa> InterviewQas { get; set; } = new List<InterviewQa>();

    public virtual JobListing? JobListing { get; set; }

    public virtual User User { get; set; } = null!;
}
