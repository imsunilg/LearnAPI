using FluentValidation;
using LearnAPI.Controllers;

namespace LearnAPI.Application.Validators;

public sealed class ClaimRequestValidator : AbstractValidator<ClaimRequest>
{
    public ClaimRequestValidator()
    {
        RuleFor(x => x.PolicyId).GreaterThan(0).WithMessage("Select the policy.");
        RuleFor(x => x.ClaimType).NotEmpty().MaximumLength(30);
        RuleFor(x => x.IncidentDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).WithMessage("The incident date cannot be in the future.");
        RuleFor(x => x.Description).NotEmpty().MinimumLength(10).MaximumLength(1000);
        RuleFor(x => x.IncidentLocation!).MaximumLength(250).When(x => x.IncidentLocation is not null);
        RuleFor(x => x.ClaimedAmount).GreaterThan(0m).LessThanOrEqualTo(1_000_000_000m);
    }
}

public sealed class ClaimStatusRequestValidator : AbstractValidator<ClaimStatusRequest>
{
    private static readonly string[] Allowed = ["Under Review", "Documents Pending", "Approved", "Rejected", "Settled", "Withdrawn"];

    public ClaimStatusRequestValidator()
    {
        RuleFor(x => x.Status).Must(s => Allowed.Contains(s)).WithMessage("Unknown claim status.");
        RuleFor(x => x.Remarks).NotEmpty().When(x => x.Status is "Rejected" or "Documents Pending").WithMessage("Please explain your decision.");
        RuleFor(x => x.ApprovedAmount).NotNull().GreaterThan(0m).When(x => x.Status == "Approved").WithMessage("Enter the approved amount.");
        RuleFor(x => x.Remarks!).MaximumLength(500).When(x => x.Remarks is not null);
    }
}
