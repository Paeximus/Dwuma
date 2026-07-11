using Dwuma.Models.Data.DwumaContext;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services;

public sealed class JobInteractionService
{
    private readonly DwumaContext _context;
    private readonly InteractionRatingService _ratingService;

    public JobInteractionService(
        DwumaContext context,
        InteractionRatingService ratingService)
    {
        _context = context;
        _ratingService = ratingService;
    }

    public async Task RecordClickAsync(
        int userId,
        int jobListingId,
        CancellationToken cancellationToken = default)
    {
        JobInteraction interaction =
            await GetOrCreateInteractionAsync(
                userId,
                jobListingId,
                cancellationToken);

        interaction.Clicked = true;

        await RecalculateRatingAsync(
            interaction,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordSaveAsync(
        int userId,
        int jobListingId,
        CancellationToken cancellationToken = default)
    {
        JobInteraction interaction =
            await GetOrCreateInteractionAsync(
                userId,
                jobListingId,
                cancellationToken);

        interaction.Saved = true;

        await RecalculateRatingAsync(
            interaction,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordDismissAsync(
        int userId,
        int jobListingId,
        CancellationToken cancellationToken = default)
    {
        JobInteraction interaction =
            await GetOrCreateInteractionAsync(
                userId,
                jobListingId,
                cancellationToken);

        interaction.Dismissed = true;

        await RecalculateRatingAsync(
            interaction,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<JobInteraction> GetOrCreateInteractionAsync(
        int userId,
        int jobListingId,
        CancellationToken cancellationToken)
    {
        bool userExists = await _context.Users.AnyAsync(
    user => user.Id == userId,
    cancellationToken);

        if (!userExists)
        {
            throw new InvalidOperationException(
                $"User with ID {userId} was not found.");
        }

        bool jobExists = await _context.JobListings.AnyAsync(
            job => job.Id == jobListingId,
            cancellationToken);

        if (!jobExists)
        {
            throw new InvalidOperationException(
                $"Job listing with ID {jobListingId} was not found.");
        }
        JobInteraction? interaction =
            await _context.JobInteractions
                .FirstOrDefaultAsync(
                    item =>
                        item.UserId == userId &&
                        item.JobListingId == jobListingId,
                    cancellationToken);

        if (interaction is not null)
            return interaction;

        interaction = new JobInteraction
        {
            UserId = userId,
            JobListingId = jobListingId,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _context.JobInteractions.Add(interaction);

        return interaction;
    }

    private async Task RecalculateRatingAsync(
        JobInteraction interaction,
        CancellationToken cancellationToken)
    {
        bool applied =
            await _context.Applications.AnyAsync(
                application =>
                    application.UserId == interaction.UserId &&
                    application.JobListingId == interaction.JobListingId,
                cancellationToken);

        interaction.Rating = _ratingService.Calculate(
            clicked: interaction.Clicked,
            saved: interaction.Saved,
            applied: applied,
            dismissed: interaction.Dismissed);

        interaction.LastUpdated = DateTime.UtcNow;
    }

    public async Task RefreshRatingAsync(
    int userId,
    int jobListingId,
    CancellationToken cancellationToken = default)
    {
        JobInteraction interaction =
            await GetOrCreateInteractionAsync(
                userId,
                jobListingId,
                cancellationToken);

        await RecalculateRatingAsync(
            interaction,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }


}