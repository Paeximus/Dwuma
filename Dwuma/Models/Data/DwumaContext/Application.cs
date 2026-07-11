using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class Application
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int JobListingId { get; set; }

    public int? CvDocumentId { get; set; }

    public string? Status { get; set; }

    public bool? AutoSubmitted { get; set; }

    public DateTime? AppliedAt { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual CvDocument? CvDocument { get; set; }

    public virtual JobListing JobListing { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
