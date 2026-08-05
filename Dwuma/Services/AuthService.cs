using Dwuma.Models.Authentication;
using Dwuma.Models.Data.DwumaContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services;

public sealed class AuthService
{
    private readonly DwumaContext _context;
    private readonly JwtTokenService _jwtTokenService;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        DwumaContext context,
        JwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _logger = logger;

        _passwordHasher =
            new PasswordHasher<User>();
    }

    public async Task<AuthResponse> RegisterAsync(
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
            Email = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password);

        _context.Users.Add(user);

        int savedRows =
            await _context.SaveChangesAsync(
                cancellationToken);

        _logger.LogInformation(
            "Registration saved {SavedRows} row(s). User ID: {UserId}",
            savedRows,
            user.Id);

        GeneratedToken generatedToken = _jwtTokenService.CreateToken(user);

        _logger.LogInformation(
            "New user registered with ID {UserId}.",
            user.Id);

        return MapResponse(user, generatedToken);
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
            FullName = user.FullName,
            Email = user.Email,
            Token = generatedToken.Value,
            ExpiresAt =
                generatedToken.ExpiresAt
        };
    }
}