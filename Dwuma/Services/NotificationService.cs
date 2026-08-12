using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services;

public sealed class NotificationService
{
    private readonly DwumaContext _dbContext;
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailService _emailService;

    public NotificationService(
        DwumaContext dbContext,
        ILogger<NotificationService> logger,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _emailService = emailService;
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

        await SendNotificationEmailAsync(
            userId,
            notification.NotificationType,
            notification.Content,
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
                "New job matches",

            "interview" =>
                "Interview feedback ready",

            "cv" =>
                "Your tailored CV is ready",

            "skills" =>
                "Skills analysis ready",

            _ =>
                "DWUMA update"
        };
    }

    public async Task<Notification> CreatePersonalizedAsync(
    int userId,
    string notificationType,
    string message,
    CancellationToken cancellationToken = default)
    {
        User? user =
            await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Id == userId,
                    cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "User was not found.");
        }

        string fullName =
            user.FullName?.Trim()
            ?? string.Empty;

        string firstName =
            string.IsNullOrWhiteSpace(fullName)
                ? "there"
                : fullName
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()
                    ?? "there";

        string personalizedContent =
            $"{firstName}, {message}";

        var notification =
            new Notification
            {
                UserId = userId,

                Content =
                    personalizedContent,

                NotificationType =
                    notificationType,

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
        await SendNotificationEmailAsync(
            userId,
            notificationType,
            personalizedContent,
            cancellationToken);

        _logger.LogInformation(
            "Created personalized {NotificationType} notification for user {UserId}.",
            notificationType,
            userId);

        return notification;
    }

    private async Task SendNotificationEmailAsync(
    int userId,
    string? notificationType,
    string message,
    CancellationToken cancellationToken)
    {
        try
        {
            User? user =
                await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.Id == userId,
                        cancellationToken);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning(
                    "Email notification skipped because user {UserId} has no email address.",
                    userId);

                return;
            }

            string subject =
                GetNotificationTitle(
                    notificationType);

            string firstName =
                string.IsNullOrWhiteSpace(user.FullName)
                    ? "there"
                    : user.FullName
                        .Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()
                        ?? "there";

            string cleanMessage = message.Trim();

            string namePrefix =
                $"{firstName},";

            if (cleanMessage.StartsWith(
                namePrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                cleanMessage =
                    cleanMessage[namePrefix.Length..]
                        .TrimStart();
            }

            string emailBody = $"""
            Hi {firstName},

            {cleanMessage}

            You can log in to DWUMA to view more details.

            Regards,
            DWUMA
            """;

            await _emailService.SendEmailAsync(
                user.Email,
                subject,
                emailBody,
                cancellationToken);

            _logger.LogInformation(
                "Notification email sent to user {UserId} for {NotificationType}.",
                userId,
                notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send notification email to user {UserId}.",
                userId);
        }
    }
}