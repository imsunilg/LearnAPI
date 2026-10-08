using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class AgentRequestValidator : AbstractValidator<AgentRequest>
{
    public static readonly string[] Statuses = ["Active", "Suspended", "Terminated"];

    public AgentRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^[6-9]\d{9}$").WithMessage("Enter a valid 10-digit mobile number");
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CommissionPercent).InclusiveBetween(0m, 100m);
        RuleFor(x => x.Status!).Must(s => Statuses.Contains(s)).When(x => !string.IsNullOrEmpty(x.Status));
        RuleFor(x => x.City!).MaximumLength(80).When(x => x.City is not null);
        RuleFor(x => x.State!).MaximumLength(80).When(x => x.State is not null);
    }
}
