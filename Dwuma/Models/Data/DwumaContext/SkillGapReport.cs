using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class SkillGapReport
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string CareerPath { get; set; } = null!;

    public DateTime? GeneratedAt { get; set; }

    public virtual ICollection<SkillGapItem> SkillGapItems { get; set; } = new List<SkillGapItem>();

    public virtual User User { get; set; } = null!;
}
