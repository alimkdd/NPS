using FluentValidation;
using NewsletterPreferences.Application.DTOs;

namespace NewsletterPreferences.Application.Validators;

internal class UpsertSubscriptionRequestValidator : AbstractValidator<UpsertSubscriptionRequest>
{
    public UpsertSubscriptionRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255);

        RuleFor(x => x.Organisation)
            .MaximumLength(255)
            .When(x => x.Organisation is not null);

        RuleFor(x => x.SubscriberTypeId)
            .GreaterThan(0).WithMessage("Please select a subscriber type.");

        RuleFor(x => x.CommunicationPreferenceIds)
            .NotNull()
            .NotEmpty().WithMessage("At least one communication preference is required.");

        RuleFor(x => x.InterestIds)
            .NotNull()
            .NotEmpty().WithMessage("At least one newsletter interest is required.");

        RuleFor(x => x.ConsentGiven)
            .Equal(true).WithMessage("You must consent to receive communications.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.PostalAddress)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.PostalAddress));
    }
}
