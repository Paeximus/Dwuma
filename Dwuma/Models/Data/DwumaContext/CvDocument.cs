using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class CvDocument
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? JobListingId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string? FileType { get; set; }

    public bool? IsBaseCv { get; set; }

    public string? Changelog { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual JobListing? JobListing { get; set; }

    public virtual User User { get; set; } = null!;
}
