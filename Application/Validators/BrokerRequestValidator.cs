using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class BrokerRequestValidator : AbstractValidator<BrokerRequest>
{
    public BrokerRequestValidator()
    {
        RuleFor(x => x.FirmName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^[6-9]\d{9}$").WithMessage("Enter a valid 10-digit mobile number");
        RuleFor(x => x.IrdaiRegistrationNo).NotEmpty().MaximumLength(30);
        RuleFor(x => x.GstNumber!).Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$").WithMessage("Enter a valid GSTIN")
            .When(x => !string.IsNullOrEmpty(x.GstNumber));
        RuleFor(x => x.CommissionPercent).InclusiveBetween(0m, 100m);
    }
}
