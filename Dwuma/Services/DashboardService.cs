using Dwuma.Models.Dashboard;
using Dwuma.Models.Data.DwumaContext;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services;

public sealed class DashboardService
{
    private readonly DwumaContext _dbContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        DwumaContext dbContext,
        ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DashboardResponse> GetDashboardAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var response = new DashboardResponse();

        // We will fill these step by step.
        response.User =
            await GetUserAsync(
                userId,
                cancellationToken);

        response.UnreadNotifications =
            await GetUnreadNotificationCountAsync(
                userId,
                cancellationToken);

        response.LatestInterview =
            await GetLatestInterviewAsync(
                userId,
                cancellationToken);

        response.TrendingSkills =
            await GetTrendingSkillsAsync(
                userId,
                cancellationToken);

        response.RecommendedJobs =
            await GetRecommendedJobsAsync(
                userId,
                cancellationToken);

        return response;
    }

    private async Task<DashboardUserResponse> GetUserAsync(
    int userId,
    CancellationToken cancellationToken)
    {
        User? user =
            await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Id == userId,
                    cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "User was not found.");
        }

        Profile? profile =
            await _dbContext.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.UserId == userId,
                    cancellationToken);

        string fullName =
            user.FullName?.Trim()
            ?? string.Empty;

        string firstName =
            string.IsNullOrWhiteSpace(fullName)
                ? "User"
                : fullName
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()
                    ?? "User";

        return new DashboardUserResponse
        {
            FirstName = firstName,

            CareerField =
                profile?.FieldOfStudy
        };
    }

    private async Task<int> GetUnreadNotificationCountAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(
                notification =>
                    notification.UserId == userId &&
                    notification.WasEngaged != true,
                cancellationToken);
    }

    private async Task<DashboardInterviewResponse?>
    GetLatestInterviewAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        InterviewSession? latestSession =
            await _dbContext.InterviewSessions
                .AsNoTracking()
                .Include(session =>
                    session.InterviewQas)
                .Where(session =>
                    session.UserId == userId &&
                    session.CompletedAt != null)
                .OrderByDescending(session =>
                    session.CompletedAt)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (latestSession == null)
        {
            return null;
        }

        List<InterviewQa> answeredQuestions =
            latestSession.InterviewQas
                .Where(qa =>
                    qa.Score.HasValue)
                .ToList();

        int score = 0;

        if (answeredQuestions.Count > 0)
        {
            double averageScore =
                answeredQuestions
                    .Average(qa =>
                        qa.Score!.Value);

            score =
                (int)Math.Round(
                    averageScore);
        }

        string? feedback =
            latestSession.InterviewQas
                .Where(qa =>
                    !string.IsNullOrWhiteSpace(
                        qa.Feedback))
                .OrderBy(qa =>
                    qa.QuestionOrder)
                .Select(qa =>
                    qa.Feedback!.Trim())
                .FirstOrDefault();

        return new DashboardInterviewResponse
        {
            Completed = true,

            Score = score,

            Message =
                GetInterviewMessage(
                    score),

            Feedback =
                feedback,

            CompletedAt =
                latestSession.CompletedAt
        };
    }

    private static string GetInterviewMessage(
    int score)
    {
        return score switch
        {
            >= 80 => "Excellent work",
            >= 60 => "Well done",
            >= 40 => "Good progress",
            _ => "Keep practising"
        };
    }

    private async Task<List<DashboardSkillResponse>>
     GetTrendingSkillsAsync(
         int userId,
         CancellationToken cancellationToken)
    {
        Profile? profile =
            await _dbContext.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.UserId == userId,
                    cancellationToken);

        string? careerField =
            profile?.FieldOfStudy;

        IQueryable<JobListing> jobsQuery =
            _dbContext.JobListings
                .AsNoTracking()
                .Where(job =>
                    job.Status == "Active" &&
                    !string.IsNullOrWhiteSpace(
                        job.RequiredSkills));

        if (!string.IsNullOrWhiteSpace(
                careerField))
        {
            string field =
                careerField.Trim();

            jobsQuery =
                jobsQuery.Where(job =>
                    job.Title.Contains(field) ||
                    (
                        job.Industry != null &&
                        job.Industry.Contains(field)
                    ) ||
                    (
                        job.Description != null &&
                        job.Description.Contains(field)
                    ));
        }

        List<string> requiredSkills =
            await jobsQuery
                .OrderByDescending(job =>
                    job.PostedAt ??
                    job.DiscoveredAt)
                .Take(50)
                .Select(job =>
                    job.RequiredSkills!)
                .ToListAsync(
                    cancellationToken);

        if (requiredSkills.Count == 0)
        {
            return [];
        }

        var skillCounts =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        foreach (string skillsText
                 in requiredSkills)
        {
            string[] skills =
                skillsText.Split(
                    new[]
                    {
                    ',',
                    ';',
                    '|'
                    },
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            foreach (string skill in skills)
            {
                string cleanedSkill =
                    skill.Trim();

                if (string.IsNullOrWhiteSpace(
                        cleanedSkill))
                {
                    continue;
                }

                if (skillCounts.ContainsKey(
                        cleanedSkill))
                {
                    skillCounts[cleanedSkill]++;
                }
                else
                {
                    skillCounts[cleanedSkill] = 1;
                }
            }
        }

        int total =
            skillCounts.Values.Sum();

        if (total == 0)
        {
            return [];
        }

        List<DashboardSkillResponse> result =
            skillCounts
                .OrderByDescending(pair =>
                    pair.Value)
                .Take(3)
                .Select(pair =>
                    new DashboardSkillResponse
                    {
                        Name = pair.Key,

                        Value =
                            (int)Math.Round(
                                pair.Value * 100.0 /
                                total)
                    })
                .ToList();

        return result;
    }

    private async Task<List<DashboardJobResponse>>
    GetRecommendedJobsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        Profile? profile =
            await _dbContext.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.UserId == userId,
                    cancellationToken);

        string? careerField =
            profile?.FieldOfStudy;

        IQueryable<JobListing> jobsQuery =
            _dbContext.JobListings
                .AsNoTracking()
                .Where(job =>
                    job.Status == "Active");

        if (!string.IsNullOrWhiteSpace(
                careerField))
        {
            string field =
                careerField.Trim();

            jobsQuery =
                jobsQuery.Where(job =>
                    job.Title.Contains(field) ||
                    (
                        job.Industry != null &&
                        job.Industry.Contains(field)
                    ) ||
                    (
                        job.Description != null &&
                        job.Description.Contains(field)
                    ));
        }

        List<DashboardJobResponse> jobs =
    await jobsQuery
        .OrderByDescending(job =>
            job.PostedAt ??
            job.DiscoveredAt)
        .Take(5)
        .Select(job =>
            new DashboardJobResponse
            {
                Id = job.Id,
                Title = job.Title,
                Company = job.Company,
                Location = job.Location,
                JobType = job.JobType,
                IsRemote = job.IsRemote,
                ApplyUrl = job.SourceUrl,
                PostedAt = job.PostedAt
            })
        .ToListAsync(
            cancellationToken);

        if (jobs.Count == 0)
        {
            jobs =
                await _dbContext.JobListings
                    .AsNoTracking()
                    .Where(job =>
                        job.Status == "Active")
                    .OrderByDescending(job =>
                        job.PostedAt ??
                        job.DiscoveredAt)
                    .Take(5)
                    .Select(job =>
                        new DashboardJobResponse
                        {
                            Id = job.Id,
                            Title = job.Title,
                            Company = job.Company,
                            Location = job.Location,
                            JobType = job.JobType,
                            IsRemote = job.IsRemote,
                            ApplyUrl = job.SourceUrl,
                            PostedAt = job.PostedAt
                        })
                    .ToListAsync(
                        cancellationToken);
        }

        return jobs; ;
    }
}