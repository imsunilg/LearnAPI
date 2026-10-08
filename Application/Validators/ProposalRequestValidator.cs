using System.Text.Json;
using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class ProposalRequestValidator : AbstractValidator<ProposalRequest>
{
    public ProposalRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Select a customer.");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Select a plan.");
        RuleFor(x => x.PolicyTermYears).InclusiveBetween(1, 5);
        RuleFor(x => x.StartDate!.Value).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).When(x => x.StartDate.HasValue)
            .WithMessage("Start date cannot be in the past.");
        RuleFor(x => x.Details).Must(d => d.ValueKind == JsonValueKind.Object).WithMessage("Details must be an object.");
        RuleFor(x => x.Remarks!).MaximumLength(500).When(x => x.Remarks is not null);
    }
}

public sealed class DecisionRequestValidator : AbstractValidator<DecisionRequest>
{
    public static readonly string[] Decisions = ["review", "approve", "reject"];

    public DecisionRequestValidator()
    {
        RuleFor(x => x.Decision).Must(d => Decisions.Contains(d?.ToLowerInvariant())).WithMessage("Decision must be review, approve or reject.");
        RuleFor(x => x.Remarks).NotEmpty().When(x => string.Equals(x.Decision, "reject", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Please give a reason for rejecting.");
        RuleFor(x => x.Remarks!).MaximumLength(500).When(x => x.Remarks is not null);
    }
}

public sealed class SubmitProposalRequestValidator : AbstractValidator<SubmitProposalRequest>
{
    public SubmitProposalRequestValidator()
    {
        RuleFor(x => x.Proposal).NotNull().SetValidator(new ProposalRequestValidator());
        RuleFor(x => x.Proposal.Partial).Equal(false).WithMessage("An incomplete proposal cannot be submitted.");
    }
}