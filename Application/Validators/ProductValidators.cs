using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class ProductTypeRequestValidator : AbstractValidator<ProductTypeRequest>
{
    public ProductTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Matches("^[A-Z]{3,20}$").WithMessage("3-20 uppercase letters, e.g. MOTOR.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Description!).MaximumLength(250).When(x => x.Description is not null);
    }
}

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ProductTypeId).GreaterThan(0).WithMessage("Select a product type.");
        RuleFor(x => x.Description!).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.BaseRate).GreaterThan(0m).LessThanOrEqualTo(100m);
        RuleFor(x => x.MinSumInsured!.Value).GreaterThanOrEqualTo(0m).When(x => x.MinSumInsured.HasValue);
        RuleFor(x => x.MaxSumInsured).GreaterThan(0m);
        RuleFor(x => x.MaxSumInsured).GreaterThanOrEqualTo(x => x.MinSumInsured ?? 0m).WithMessage("Maximum sum insured must be at least the minimum.");
        RuleFor(x => x.EffectiveTo!.Value).GreaterThanOrEqualTo(x => x.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Effective-to date cannot be before effective-from.")
            .When(x => x.EffectiveTo.HasValue);
    }
}
