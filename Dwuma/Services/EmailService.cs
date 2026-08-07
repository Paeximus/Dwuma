using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Dwuma.Services;

public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        string host =
            _configuration["Email:Host"]
            ?? throw new InvalidOperationException(
                "Email host is not configured.");

        int port =
            int.TryParse(
                _configuration["Email:Port"],
                out int configuredPort)
                ? configuredPort
                : 587;

        string username =
            _configuration["Email:Username"]
            ?? throw new InvalidOperationException(
                "Email username is not configured.");

        string password =
            _configuration["Email:Password"]
            ?? throw new InvalidOperationException(
                "Email password is not configured.");

        string fromEmail =
            _configuration["Email:FromEmail"]
            ?? username;

        string fromName =
            _configuration["Email:FromName"]
            ?? "DWUMA";

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                fromName,
                fromEmail));

        message.To.Add(
            MailboxAddress.Parse(
                recipientEmail));

        message.Subject =
            "Verify your DWUMA email address";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;">
                    <h2>Verify your email address</h2>

                    <p>
                        Thank you for creating a DWUMA account.
                        Click the button below to verify your email.
                    </p>

                    <p style="margin:30px 0;">
                        <a
                            href="{verificationLink}"
                            style="
                                background:#850808;
                                color:white;
                                padding:12px 22px;
                                border-radius:24px;
                                text-decoration:none;
                                display:inline-block;
                            ">
                            Verify email
                        </a>
                    </p>

                    <p>
                        This link expires in 30 minutes.
                    </p>

                    <p>
                        If you did not create this account,
                        you can ignore this email.
                    </p>
                </div>
                """
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient
        {
            CheckCertificateRevocation = false
        };

        await smtpClient.ConnectAsync(
            host,
            port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        await smtpClient.AuthenticateAsync(
            username,
            password,
            cancellationToken);

        await smtpClient.SendAsync(
            message,
            cancellationToken);

        await smtpClient.DisconnectAsync(
            true,
            cancellationToken);

        _logger.LogInformation(
            "Verification email sent to {Email}.",
            recipientEmail);
    }
}