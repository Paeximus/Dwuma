using System.Security.Claims;

namespace Dwuma.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(
        this ClaimsPrincipal user)
    {
        string? userIdValue =
            user.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdValue,
                out int userId))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user ID is missing or invalid.");
        }

        return userId;
    }
}