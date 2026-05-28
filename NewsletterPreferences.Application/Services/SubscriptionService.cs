using FluentValidation;
using NewsletterPreferences.Application.Common;
using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Application.Interfaces;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Domain.ValueObjects;

namespace NewsletterPreferences.Application.Services;

public class SubscriptionService(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionReadRepository subscriptionReadRepository,
    ILookupRepository lookupRepository,
    IUnitOfWork unitOfWork,
    IValidator<UpsertSubscriptionRequest> validator) : ISubscriptionService
{
    public async Task<Result<UpsertSubscriptionResult>> UpsertAsync(
        UpsertSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return Result<UpsertSubscriptionResult>.ValidationFailure(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList());

        var allPrefs = await lookupRepository.GetCommunicationPreferencesAsync(cancellationToken);
        var selectedCodes = allPrefs
            .Where(p => request.CommunicationPreferenceIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToHashSet();

        var conditionalErrors = new List<string>();

        if ((selectedCodes.Contains("PHONE") || selectedCodes.Contains("SMS"))
            && string.IsNullOrWhiteSpace(request.PhoneNumber))
            conditionalErrors.Add("Phone number is required when Phone or SMS is selected.");

        if (selectedCodes.Contains("POST") && string.IsNullOrWhiteSpace(request.PostalAddress))
            conditionalErrors.Add("Postal address is required when Post is selected.");

        if (conditionalErrors.Count > 0)
            return Result<UpsertSubscriptionResult>.ValidationFailure(conditionalErrors);

        if (!await lookupRepository.SubscriberTypeExistsAsync(request.SubscriberTypeId, cancellationToken))
            return Result<UpsertSubscriptionResult>.ValidationFailure(["Invalid subscriber type."]);

        if (!await lookupRepository.AllCommunicationPreferencesExistAsync(request.CommunicationPreferenceIds, cancellationToken))
            return Result<UpsertSubscriptionResult>.ValidationFailure(["One or more communication preferences are invalid."]);

        if (!await lookupRepository.AllInterestsExistAsync(request.InterestIds, cancellationToken))
            return Result<UpsertSubscriptionResult>.ValidationFailure(["One or more newsletter interests are invalid."]);

        Email email;
        try { email = Email.Create(request.Email); }
        catch (ArgumentException ex)
        {
            return Result<UpsertSubscriptionResult>.ValidationFailure([ex.Message]);
        }

        var existing = await subscriptionRepository.GetByEmailAsync(email.Value, cancellationToken);
        bool isUpdate = existing is not null;
        Guid subscriptionId;

        if (isUpdate)
        {
            existing!.UpdatePreferences(
                request.FirstName, request.LastName, request.Organisation,
                request.SubscriberTypeId, request.PhoneNumber, request.PostalAddress,
                request.CommunicationPreferenceIds, request.InterestIds);

            await subscriptionRepository.UpdateAsync(existing, cancellationToken);
            subscriptionId = existing.Id;
        }
        else
        {
            var subscription = Subscription.Create(
                request.FirstName, request.LastName, email, request.Organisation,
                request.SubscriberTypeId, request.PhoneNumber, request.PostalAddress,
                request.ConsentGiven, request.CommunicationPreferenceIds, request.InterestIds);

            await subscriptionRepository.AddAsync(subscription, cancellationToken);
            subscriptionId = subscription.Id;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpsertSubscriptionResult>.Success(new UpsertSubscriptionResult
        {
            SubscriptionId = subscriptionId,
            IsUpdate = isUpdate
        });
    }

    public async Task<Result<SubscriptionResponse>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var response = await subscriptionReadRepository.GetByIdAsync(id, cancellationToken);

        return response is null
            ? Result<SubscriptionResponse>.Failure("Subscription not found.")
            : Result<SubscriptionResponse>.Success(response);
    }

    public async Task<Result<PagedResult<SubscriptionResponse>>> GetPagedAsync(
        SubscriptionFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 6);

        var (items, totalCount) = await subscriptionReadRepository.GetPagedAsync(
            filter.SearchTerm,
            filter.SubscriberTypeId,
            filter.CommunicationPreferenceId,
            filter.InterestId,
            page, pageSize,
            cancellationToken);

        return Result<PagedResult<SubscriptionResponse>>.Success(
            new PagedResult<SubscriptionResponse>(items, totalCount, page, pageSize));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(id, cancellationToken);

        if (subscription is null)
            return Result.Failure("Subscription not found.");

        subscription.MarkAsDeleted();
        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public Task<SubscriptionStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default) =>
        subscriptionReadRepository.GetStatsAsync(cancellationToken);

    public async Task<Result> UnsubscribeAsync(
        UnsubscribeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure("Subscription not found.");

        Email email;
        try { email = Email.Create(request.Email); }
        catch (ArgumentException)
        {
            return Result.Failure("Subscription not found.");
        }

        var existing = await subscriptionRepository.GetByEmailAsync(email.Value, cancellationToken);
        if (existing is null)
            return Result.Failure("Subscription not found.");

        existing.MarkAsDeleted();
        await subscriptionRepository.UpdateAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
