using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class JobInteraction
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int JobListingId { get; set; }

    public bool Clicked { get; set; }

    public bool Saved { get; set; }

    public bool Dismissed { get; set; }

    public float SkillMatch { get; set; }

    public float LocationMatch { get; set; }

    public float ExperienceMatch { get; set; }

    public float IndustryMatch { get; set; }

    public float SalaryMatch { get; set; }

    public float JobAgeDays { get; set; }

    public float Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastUpdated { get; set; }

    public virtual JobListing JobListing { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
