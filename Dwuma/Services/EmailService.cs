using System.Net.Http.Json;
using System.Text.Json;

namespace Dwuma.Services;

public sealed class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        string apiKey =
            _configuration["Brevo:ApiKey"]
            ?? throw new InvalidOperationException(
                "Brevo API key is not configured.");

        string fromEmail =
            _configuration["Email:FromEmail"]
            ?? throw new InvalidOperationException(
                "Email sender address is not configured.");

        string fromName =
            _configuration["Email:FromName"]
            ?? "DWUMA";

        var payload = new
        {
            sender = new
            {
                name = fromName,
                email = fromEmail
            },

            to = new[]
            {
                new
                {
                    email = recipientEmail
                }
            },

            subject =
                "Verify your DWUMA email address",

            htmlContent =
                BuildVerificationHtml(
                    verificationLink)
        };

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email");

        request.Headers.Add(
            "api-key",
            apiKey);

        request.Headers.Add(
            "accept",
            "application/json");

        request.Content =
            JsonContent.Create(payload);

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Brevo email failed. Status: {StatusCode}. Response: {Response}",
                response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"Brevo email delivery failed with HTTP {(int)response.StatusCode}.");
        }

        _logger.LogInformation(
            "Verification email sent successfully to {Email}.",
            recipientEmail);
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

    public async Task SendEmailAsync(
    string recipientEmail,
    string subject,
    string message,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException(
                "Recipient email is required.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException(
                "Email subject is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Email message is required.");
        }

        // Send using the same HTTP email-provider logic
        // already used by your verification/password-reset emails.
    }
}