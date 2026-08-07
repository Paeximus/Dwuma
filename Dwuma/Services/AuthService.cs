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
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        bool emailExists =
            await _context.Users
                .AnyAsync(
                    user => user.Email == email,
                    cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "An account with this email already exists.");
        }

        var user = new User
        {
            FullName = string.Empty,
            Email = email.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
             IsEmailVerified = false,
            EmailVerifiedAt = null,

            OnboardingStatus = OnboardingStatus.NotStarted,

            OnboardingCompletedAt = null,

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
            HashVerificationToken(emailVerificationToken);

        user.EmailVerificationTokenHash = hashedToken;
        user.EmailVerificationExpiresAt =
            DateTime.UtcNow.AddMinutes(30);

        string frontendBaseUrl =
            _configuration[
                "Email:FrontendBaseUrl"]
            ?? "http://localhost:5173";

        string verificationLink =
            $"{frontendBaseUrl.TrimEnd('/')}" +
            "/email-verified" +
            $"?token={Uri.EscapeDataString(emailVerificationToken)}" +
            $"&email={Uri.EscapeDataString(user.Email)}";


        await _emailService
            .SendVerificationEmailAsync(
                user.Email,
                verificationLink,
                cancellationToken);


        _logger.LogInformation(
            "Registration saved {SavedRows} row(s). User ID: {UserId}",
            savedRows,
            user.Id);

        GeneratedToken generatedToken = _jwtTokenService.CreateToken(user);

        _logger.LogInformation(
            "New user registered with ID {UserId}.",
            user.Id);

        return new RegisterResponse
        {
            Message ="Account created. Check your email to verify your account.",

            Email = user.Email
        };

    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        User? user =
            await _context.Users
                .SingleOrDefaultAsync(
                    existingUser =>
                        existingUser.Email == email,
                    cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
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

        if (result ==
            PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (result ==
            PasswordVerificationResult
                .SuccessRehashNeeded)
        {
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    request.Password);
        }

        GeneratedToken generatedToken = _jwtTokenService.CreateToken(user);

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

        if (!user.IsEmailVerified)
        {
            throw new InvalidOperationException(
                "Verify your email before logging in.");
        }

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
            //FullName = user.FullName,
            Email = user.Email,
            Token = generatedToken.Value,
            ExpiresAt =
                generatedToken.ExpiresAt
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
            ?? "http://localhost:5173";

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