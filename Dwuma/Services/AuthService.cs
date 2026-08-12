using Dwuma.Models.Auth;
using Dwuma.Models.Authentication;
using Dwuma.Models.Data.DwumaContext;
using Dwuma.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Dwuma.Services;

public sealed class AuthService
{
    private readonly DwumaContext _context;
    private readonly JwtTokenService _jwtTokenService;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        DwumaContext context,
        JwtTokenService jwtTokenService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;

        _passwordHasher =
            new PasswordHasher<User>();
    }

    public async Task<RegisterResponse> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException(
                "Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(
                "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException(
                "Password is required.");
        }

        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        string username =
            request.Username
                .Trim();

        string normalizedUsername =
            username.ToLowerInvariant();

        bool emailExists =
            await _context.Users
                .AnyAsync(
                    user =>
                        user.Email.ToLower() == email,
                    cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "An account with this email already exists.");
        }

        bool usernameExists =
            await _context.Users
                .AnyAsync(
                    user =>
                        user.FullName.ToLower() ==
                        normalizedUsername,
                    cancellationToken);

        if (usernameExists)
        {
            throw new InvalidOperationException(
                "This username is already taken. Please choose another username.");
        }

        var user =
            new User
            {
                FullName = username,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsEmailVerified = false,
                EmailVerifiedAt = null,
                OnboardingStatus =
                    OnboardingStatus.NotStarted,
                OnboardingCompletedAt = null
            };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password);

        _context.Users.Add(user);

        int savedRows =
            await _context.SaveChangesAsync(
                cancellationToken);

        string emailVerificationToken =
            GenerateEmailVerificationToken();

        string hashedToken =
            HashVerificationToken(
                emailVerificationToken);

        user.EmailVerificationTokenHash =
            hashedToken;

        user.EmailVerificationExpiresAt =
            DateTime.UtcNow.AddMinutes(30);

        await _context.SaveChangesAsync(
            cancellationToken);

        string frontendBaseUrl =
            _configuration[
                "Email:FrontendBaseUrl"]
            ?? "https://project-la6nn.vercel.app";

        string verificationLink =
            $"{frontendBaseUrl.TrimEnd('/')}" +
            "/email-verified" +
            $"?token={Uri.EscapeDataString(emailVerificationToken)}" +
            $"&email={Uri.EscapeDataString(user.Email)}";

        bool verificationEmailSent =
            false;

        try
        {
            _logger.LogInformation(
                "Sending verification email to {Email}.",
                user.Email);

            await _emailService
                .SendVerificationEmailAsync(
                    user.Email,
                    verificationLink,
                    cancellationToken);

            verificationEmailSent =
                true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Account {Email} was created, but its verification email could not be sent.",
                user.Email);
        }

        _logger.LogInformation(
            "Registration saved {SavedRows} row(s). User ID: {UserId}",
            savedRows,
            user.Id);

        return new RegisterResponse
        {
            Username = user.FullName,
            Email = user.Email,

            VerificationEmailSent =
                verificationEmailSent,

            Message =
                verificationEmailSent
                    ? "Account created. Check your email to verify your account."
                    : "Account created, but the verification email could not be sent. Use resend verification."
        };
    }

    public async Task<AuthResponse> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Login))
        {
            throw new ArgumentException(
                "Email or username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException(
                "Password is required.");
        }

        string normalizedLogin =
            request.Login
                .Trim()
                .ToLower();

        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.Email.ToLower() == normalizedLogin ||
                        u.FullName.ToLower() == normalizedLogin,
                    cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email, username or password.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "This account has no password. Please register again or reset the password.");
        }

        PasswordVerificationResult result =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "Invalid email, username or password.");
        }

        if (!user.IsEmailVerified)
        {
            throw new InvalidOperationException(
                "Verify your email before logging in.");
        }

        // Update the stored hash if ASP.NET recommends rehashing it.
        if (result ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    request.Password);

            user.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        GeneratedToken generatedToken =
            _jwtTokenService.CreateToken(user);

        _logger.LogInformation(
            "User {UserId} logged in.",
            user.Id);

        return MapResponse(
            user,
            generatedToken);
    }

    private static AuthResponse MapResponse(
    User user,
    GeneratedToken generatedToken)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Token = generatedToken.Value,
            ExpiresAt = generatedToken.ExpiresAt
        };
    }

    private static string GenerateEmailVerificationToken()
    {
        byte[] tokenBytes =
            RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashVerificationToken(
        string token)
    {
        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(token);

        byte[] hashBytes =
            SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    public async Task VerifyEmailAsync(
    VerifyEmailRequest request,
    CancellationToken cancellationToken)
    {
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        string token =
            request.Token.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Email and verification token are required.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                currentUser =>
                    currentUser.Email == email,
                cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                "The verification link is invalid.");
        }

        if (user.IsEmailVerified)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                user.EmailVerificationTokenHash))
        {
            throw new InvalidOperationException(
                "The verification link is invalid.");
        }

        if (!user.EmailVerificationExpiresAt.HasValue ||
            user.EmailVerificationExpiresAt.Value <
            DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "The verification link has expired.");
        }

        string submittedTokenHash =
            HashVerificationToken(token);

        bool tokenMatches =
            CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(
                    user.EmailVerificationTokenHash),
                Convert.FromHexString(
                    submittedTokenHash));

        if (!tokenMatches)
        {
            throw new InvalidOperationException(
                "The verification link is invalid.");
        }

        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;

        user.EmailVerificationTokenHash = null;
        user.EmailVerificationExpiresAt = null;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ResendVerificationEmailAsync(
    string email,
    CancellationToken cancellationToken)
    {
        string normalizedEmail =
            email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                currentUser =>
                    currentUser.Email ==
                    normalizedEmail,
                cancellationToken);

        // Do not reveal whether an account exists.
        if (user is null ||
            user.IsEmailVerified)
        {
            return;
        }

        string verificationToken =
            GenerateEmailVerificationToken();

        user.EmailVerificationTokenHash =
            HashVerificationToken(
                verificationToken);

        user.EmailVerificationExpiresAt =
            DateTime.UtcNow.AddMinutes(30);

        user.UpdatedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync(
            cancellationToken);

        string frontendBaseUrl =
            _configuration[
                "Email:FrontendBaseUrl"]
            ?? "https://project-la6nn.vercel.app";

        string verificationLink =
            $"{frontendBaseUrl.TrimEnd('/')}" +
            "/email-verified" +
            $"?token={Uri.EscapeDataString(verificationToken)}" +
            $"&email={Uri.EscapeDataString(user.Email)}";

        await _emailService
            .SendVerificationEmailAsync(
                user.Email,
                verificationLink,
                cancellationToken);
    }
}