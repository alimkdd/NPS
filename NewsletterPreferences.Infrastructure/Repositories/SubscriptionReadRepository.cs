using Microsoft.EntityFrameworkCore;
using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Application.Interfaces;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace NewsletterPreferences.Infrastructure.Repositories;

public class SubscriptionReadRepository(AppDbContext context) : ISubscriptionReadRepository
{
    private static readonly Expression<Func<Subscription, SubscriptionResponse>> Projection =
        s => new SubscriptionResponse
        {
            Id = s.Id,
            FirstName = s.FirstName,
            LastName = s.LastName,
            Email = s.Email,
            Organisation = s.Organisation,
            SubscriberType = new LookupItemResponse
            {
                Id = s.SubscriberType.Id,
                Name = s.SubscriberType.Name,
                Code = s.SubscriberType.Code
            },
            CommunicationPreferences = s.CommunicationPreferences
                .Select(scp => new LookupItemResponse
                {
                    Id = scp.CommunicationPreference.Id,
                    Name = scp.CommunicationPreference.Name,
                    Code = scp.CommunicationPreference.Code
                })
                .ToList(),
            Interests = s.Interests
                .Select(si => new LookupItemResponse
                {
                    Id = si.NewsletterInterest.Id,
                    Name = si.NewsletterInterest.Name,
                    Code = si.NewsletterInterest.Code
                })
                .ToList(),
            PhoneNumber = s.PhoneNumber,
            PostalAddress = s.PostalAddress,
            ConsentGiven = s.ConsentGiven,
            ConsentTimestamp = s.ConsentTimestamp,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

    public Task<SubscriptionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Subscriptions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(Projection)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<SubscriptionResponse> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm,
        int? subscriberTypeId,
        int? communicationPreferenceId,
        int? interestId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Subscriptions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(s =>
                EF.Functions.Like(s.FirstName, pattern) ||
                EF.Functions.Like(s.LastName, pattern) ||
                EF.Functions.Like(s.Email, pattern));
        }

        if (subscriberTypeId.HasValue)
            query = query.Where(s => s.SubscriberTypeId == subscriberTypeId.Value);

        if (communicationPreferenceId.HasValue)
            query = query.Where(s =>
                s.CommunicationPreferences.Any(scp => scp.CommunicationPreferenceId == communicationPreferenceId.Value));

        if (interestId.HasValue)
            query = query.Where(s =>
                s.Interests.Any(si => si.NewsletterInterestId == interestId.Value));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
