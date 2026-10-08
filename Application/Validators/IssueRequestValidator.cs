using FluentValidation;
using LearnAPI.Controllers;

namespace LearnAPI.Application.Validators;

public sealed class IssueRequestValidator : AbstractValidator<PoliciesController.IssueRequest>
{
    public IssueRequestValidator() => RuleFor(x => x.ProposalId).GreaterThan(0);
}
