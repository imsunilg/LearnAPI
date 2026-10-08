using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class MasterDataRequestValidator : AbstractValidator<MasterDataRequest>
{
    public MasterDataRequestValidator()
    {
        RuleFor(x => x.Type).NotEmpty().Matches("^[A-Z][A-Z_]{1,39}$").WithMessage("Uppercase letters and underscores, e.g. VEHICLE_MAKE.");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SortOrder!.Value).GreaterThanOrEqualTo(0).When(x => x.SortOrder.HasValue);
    }
}
