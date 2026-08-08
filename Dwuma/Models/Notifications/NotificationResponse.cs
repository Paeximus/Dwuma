namespace Dwuma.Models.Notifications;

public sealed class NotificationResponse
{
    public int Id { get; set; }

    public string Title { get; set; }
        = string.Empty;

    public string Message { get; set; }
        = string.Empty;

    public string Category { get; set; }
        = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}