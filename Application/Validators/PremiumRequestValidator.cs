using System.Text.Json;
using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class PremiumRequestValidator : AbstractValidator<PremiumRequest>
{
    public PremiumRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Select a plan.");
        RuleFor(x => x.Details).Must(d => d.ValueKind == JsonValueKind.Object).WithMessage("Details must be an object.");
    }
}
