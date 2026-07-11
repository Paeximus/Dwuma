using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class NotificationEngagement
{
    public int Id { get; set; }

    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public int? DayOfWeek { get; set; }

    public int? HourOfDay { get; set; }

    public bool? Engaged { get; set; }

    public DateTime? RecordedAt { get; set; }

    public virtual Notification Notification { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
