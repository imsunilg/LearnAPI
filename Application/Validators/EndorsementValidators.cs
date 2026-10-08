using FluentValidation;
using LearnAPI.Controllers;

namespace LearnAPI.Application.Validators;

public sealed class EndorsementRequestValidator : AbstractValidator<EndorsementRequest>
{
    private static readonly string[] Types = ["ADDRESS_CHANGE", "ADD_NOMINEE", "VEHICLE_CHANGE"];

    public EndorsementRequestValidator()
    {
        RuleFor(x => x.PolicyId).GreaterThan(0);
        RuleFor(x => x.Type).Must(t => Types.Contains(t?.ToUpperInvariant())).WithMessage("Unknown endorsement type.");
        RuleFor(x => x.EffectiveDate).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).WithMessage("The effective date cannot be in the past.");
        RuleFor(x => x.Remarks!).MaximumLength(500).When(x => x.Remarks is not null);
    }
}

public sealed class EndorsementDecisionValidator : AbstractValidator<EndorsementDecisionRequest>
{
    public EndorsementDecisionValidator()
    {
        RuleFor(x => x.Remarks).NotEmpty().When(x => !x.Approve).WithMessage("Please give a reason for rejecting.");
    }
}
