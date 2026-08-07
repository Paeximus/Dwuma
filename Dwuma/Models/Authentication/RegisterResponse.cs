namespace Dwuma.Models.Auth;

public sealed class RegisterResponse
{
    public string Message { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}