using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = null!;

    public string? NotificationType { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public bool? WasEngaged { get; set; }

    public virtual ICollection<NotificationEngagement> NotificationEngagements { get; set; } = new List<NotificationEngagement>();

    public virtual User User { get; set; } = null!;
}
