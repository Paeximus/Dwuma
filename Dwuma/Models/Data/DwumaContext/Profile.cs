using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class Profile
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Institution { get; set; }

    public string? Degree { get; set; }

    public string? FieldOfStudy { get; set; }

    public int? GraduationYear { get; set; }

    public string? GpaClassification { get; set; }

    public string? PreferredIndustries { get; set; }

    public string? JobTypePreference { get; set; }

    public string? LocationPreference { get; set; }

    public string? SalaryExpectation { get; set; }

    public string? CareerGoals { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
