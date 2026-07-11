using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class InterviewQa
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public string Question { get; set; } = null!;

    public string? UserResponse { get; set; }

    public string? ResponseMode { get; set; }

    public double? Score { get; set; }

    public string? Feedback { get; set; }

    public int? QuestionOrder { get; set; }

    public virtual InterviewSession Session { get; set; } = null!;
}
