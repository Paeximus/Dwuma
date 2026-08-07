namespace Dwuma.Services;

public static class InterviewSafetyValidator
{
    private static readonly string[] HighRiskTerms =
    [
        "meth",
        "cocaine production",
        "drug trafficking",
        "drug trafficker",
        "hitman",
        "assassin",
        "human trafficking",
        "bomb making",
        "illegal weapons",
        "illegal arms",
        "identity theft",
        "credit card fraud",
        "ransomware",
        "phishing scam",
        "malware theft"
    ];

    public static bool IsAllowedRole(
        string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        string normalized =
            role.Trim().ToLowerInvariant();

        return !HighRiskTerms.Any(term =>
            normalized.Contains(term));
    }
}