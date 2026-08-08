using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services;

public sealed class NotificationService
{
    private readonly DwumaContext _dbContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        DwumaContext dbContext,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // =========================================================
    // CREATE NOTIFICATION
    // =========================================================

    public async Task<Notification> CreateAsync(
        int userId,
        string content,
        string notificationType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Notification content is required.");
        }

        if (string.IsNullOrWhiteSpace(notificationType))
        {
            notificationType = "General";
        }

        var notification = new Notification
        {
            UserId = userId,

            Content =
                content.Trim(),

            NotificationType =
                notificationType.Trim(),

            ScheduledAt =
                DateTime.UtcNow,

            SentAt =
                DateTime.UtcNow,

            WasEngaged =
                false
        };

        _dbContext.Notifications.Add(
            notification);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Created {NotificationType} notification for user {UserId}.",
            notification.NotificationType,
            userId);

        return notification;
    }

    // =========================================================
    // GET USER NOTIFICATIONS
    // =========================================================

    public async Task<List<NotificationResponse>>
        GetUserNotificationsAsync(
            int userId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(n =>
                n.UserId == userId)
            .OrderByDescending(n =>
                n.SentAt ?? n.ScheduledAt)
            .Select(n =>
                new NotificationResponse
                {
                    Id = n.Id,

                    Title =
                        GetNotificationTitle(
                            n.NotificationType),

                    Message =
                        n.Content,

                    Category =
                        n.NotificationType
                        ?? "General",

                    IsRead =
                        n.WasEngaged
                        ?? false,

                    CreatedAt =
                        n.SentAt
                        ?? n.ScheduledAt
                        ?? DateTime.UtcNow
                })
            .ToListAsync(
                cancellationToken);
    }

    // =========================================================
    // MARK ONE AS READ
    // =========================================================

    public async Task MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Notification? notification =
            await _dbContext.Notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.Id == notificationId &&
                        n.UserId == userId,
                    cancellationToken);

        if (notification == null)
        {
            throw new KeyNotFoundException(
                "Notification was not found.");
        }

        notification.WasEngaged = true;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // MARK ALL AS READ
    // =========================================================

    public async Task MarkAllAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        List<Notification> notifications =
            await _dbContext.Notifications
                .Where(n =>
                    n.UserId == userId &&
                    n.WasEngaged != true)
                .ToListAsync(
                    cancellationToken);

        if (notifications.Count == 0)
        {
            return;
        }

        foreach (Notification notification
                 in notifications)
        {
            notification.WasEngaged = true;
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // DELETE NOTIFICATION
    // =========================================================

    public async Task DeleteAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Notification? notification =
            await _dbContext.Notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.Id == notificationId &&
                        n.UserId == userId,
                    cancellationToken);

        if (notification == null)
        {
            throw new KeyNotFoundException(
                "Notification was not found.");
        }

        _dbContext.Notifications.Remove(
            notification);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // UNREAD COUNT
    // =========================================================

    public async Task<int> GetUnreadCountAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .CountAsync(
                n =>
                    n.UserId == userId &&
                    n.WasEngaged != true,
                cancellationToken);
    }

    // =========================================================
    // TITLE MAPPING
    // =========================================================

    private static string GetNotificationTitle(
        string? notificationType)
    {
        return notificationType?
            .Trim()
            .ToLowerInvariant() switch
        {
            "jobs" =>
                "New job update",

            "interview" =>
                "Interview update",

            "cv" =>
                "CV update",

            "skills" =>
                "Skills update",

            _ =>
                "DWUMA notification"
        };
    }
}