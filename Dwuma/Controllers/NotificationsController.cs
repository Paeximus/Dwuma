using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Notifications;
using Dwuma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dwuma.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController
    : ControllerBase
{
    private readonly NotificationService
        _notificationService;

    public NotificationsController(
        NotificationService notificationService)
    {
        _notificationService =
            notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<
        List<NotificationResponse>>> GetNotifications(
        CancellationToken cancellationToken)
    {
        int userId = GetUserId();

        List<NotificationResponse> notifications =
            await _notificationService
                .GetUserNotificationsAsync(
                    userId,
                    cancellationToken);

        return Ok(notifications);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id,
        CancellationToken cancellationToken)
    {
        int userId = GetUserId();

        await _notificationService.MarkAsReadAsync(
            id,
            userId,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        int userId = GetUserId();

        await _notificationService.MarkAllAsReadAsync(
            userId,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        int userId = GetUserId();

        await _notificationService.DeleteAsync(
            id,
            userId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>>
        GetUnreadCount(
            CancellationToken cancellationToken)
    {
        int userId = GetUserId();

        int count =
            await _notificationService
                .GetUnreadCountAsync(
                    userId,
                    cancellationToken);

        return Ok(new
        {
            count
        });
    }

    [HttpPost("test")]
    public async Task<IActionResult> CreateTestNotification(
    CancellationToken cancellationToken)
    {
        int userId =
            GetUserId();

        Notification notification =
            await _notificationService.CreateAsync(
                userId,
                "This is a DWUMA test notification.",
                "Interview",
                cancellationToken);

        return Ok(new
        {
            notification.Id,
            notification.Content,
            notification.NotificationType,
            notification.SentAt,
            IsRead =
                notification.WasEngaged ?? false
        });
    }

    private int GetUserId()
    {
        string? value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                value,
                out int userId))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user could not be identified.");
        }

        return userId;
    }
}