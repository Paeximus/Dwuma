namespace Dwuma.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationLink,
        CancellationToken cancellationToken = default);
}