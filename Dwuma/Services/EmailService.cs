using Resend;

namespace Dwuma.Services;

public sealed class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IResend resend,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _resend = resend;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        string fromEmail =
            _configuration["Email:FromEmail"]
            ?? throw new InvalidOperationException(
                "Email sender address is not configured.");

        string fromName =
            _configuration["Email:FromName"]
            ?? "DWUMA";

        var message = new EmailMessage
        {
            From =
                $"{fromName} <{fromEmail}>",

            Subject =
                "Verify your DWUMA email address",

            HtmlBody = BuildVerificationHtml(
                verificationLink)
        };

        message.To.Add(recipientEmail);

        try
        {
            var response =
                await _resend.EmailSendAsync(
                    message,
                    cancellationToken);

            _logger.LogInformation(
                "Verification email sent to {Email}. Resend ID: {EmailId}",
                recipientEmail,
                response.Content);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Resend email delivery failed for {Email}.",
                recipientEmail);

            throw;
        }
    }

    private static string BuildVerificationHtml(
        string verificationLink)
    {
        string safeLink =
            System.Net.WebUtility.HtmlEncode(
                verificationLink);

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta
                    name="viewport"
                    content="width=device-width, initial-scale=1.0">
                <title>Verify your email</title>
            </head>

            <body style="
                margin:0;
                padding:0;
                background:#f8f5f5;
                font-family:Arial,sans-serif;
                color:#222;
            ">
                <table
                    role="presentation"
                    width="100%"
                    cellspacing="0"
                    cellpadding="0"
                    style="padding:40px 16px;">
                    <tr>
                        <td align="center">
                            <table
                                role="presentation"
                                width="100%"
                                cellspacing="0"
                                cellpadding="0"
                                style="
                                    max-width:600px;
                                    background:#ffffff;
                                    border-radius:16px;
                                    padding:40px;
                                ">
                                <tr>
                                    <td>
                                        <h1 style="
                                            margin:0 0 20px;
                                            color:#850808;
                                            font-size:28px;
                                        ">
                                            Verify your email
                                        </h1>

                                        <p style="
                                            font-size:16px;
                                            line-height:1.6;
                                        ">
                                            Thank you for creating a
                                            DWUMA account.
                                        </p>

                                        <p style="
                                            font-size:16px;
                                            line-height:1.6;
                                        ">
                                            Verify your email address
                                            to continue to onboarding.
                                        </p>

                                        <p style="
                                            margin:32px 0;
                                        ">
                                            <a
                                                href="{safeLink}"
                                                style="
                                                    display:inline-block;
                                                    background:#850808;
                                                    color:#ffffff;
                                                    padding:14px 24px;
                                                    border-radius:999px;
                                                    text-decoration:none;
                                                    font-weight:600;
                                                ">
                                                Verify email
                                            </a>
                                        </p>

                                        <p style="
                                            font-size:14px;
                                            color:#666;
                                            line-height:1.6;
                                        ">
                                            This link expires in
                                            30 minutes.
                                        </p>

                                        <p style="
                                            font-size:14px;
                                            color:#666;
                                            line-height:1.6;
                                        ">
                                            If you did not create this
                                            account, ignore this email.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}