using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class Skill
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string SkillName { get; set; } = null!;

    public string? SkillType { get; set; }

    public string? ProficiencyLevel { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
