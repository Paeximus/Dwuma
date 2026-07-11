using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class SkillGapItem
{
    public int Id { get; set; }

    public int ReportId { get; set; }

    public string SkillName { get; set; } = null!;

    public string? GapStatus { get; set; }

    public string? RequiredLevel { get; set; }

    public string? ResourceName { get; set; }

    public string? ResourceUrl { get; set; }

    public virtual SkillGapReport Report { get; set; } = null!;
}
