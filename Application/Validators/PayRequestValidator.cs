using FluentValidation;
using LearnAPI.Controllers;

namespace LearnAPI.Application.Validators;

public sealed class PayRequestValidator : AbstractValidator<PayRequest>
{
    public PayRequestValidator()
    {
        RuleFor(x => x.PolicyId).GreaterThan(0);
        RuleFor(x => x.Mode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Amount!.Value).GreaterThan(0m).When(x => x.Amount.HasValue);
        RuleFor(x => x.Reference!).MaximumLength(60).When(x => x.Reference is not null);
    }
}
