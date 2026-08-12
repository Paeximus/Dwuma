namespace Dwuma.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationLink,
        CancellationToken cancellationToken = default);

    Task SendEmailAsync(
    string recipientEmail,
    string subject,
    string message,
    CancellationToken cancellationToken = default);
}