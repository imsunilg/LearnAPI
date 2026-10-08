using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

/// <summary>Rules mirror the Zod schema in LearnWEB (src/features/customers/customerSchema.ts) - keep them in sync.</summary>
public sealed class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    public static readonly string[] Genders = ["Male", "Female", "Other"];
    public static readonly string[] KycStatuses = ["Pending", "Verified", "Rejected"];

    public CustomerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^[6-9]\d{9}$").WithMessage("Mobile must be a valid 10-digit Indian number.");
        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Date of birth must be in the past.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today).AddYears(-18)).WithMessage("Customer must be at least 18 years old.");
        RuleFor(x => x.Gender!).Must(g => Genders.Contains(g)).When(x => !string.IsNullOrEmpty(x.Gender));
        RuleFor(x => x.PanNumber!).Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]$").WithMessage("PAN must look like ABCDE1234F.")
            .When(x => !string.IsNullOrEmpty(x.PanNumber));
        RuleFor(x => x.Pincode!).Matches(@"^\d{6}$").WithMessage("Pincode must be 6 digits.").When(x => !string.IsNullOrEmpty(x.Pincode));
        RuleFor(x => x.KycStatus!).Must(k => KycStatuses.Contains(k)).When(x => !string.IsNullOrEmpty(x.KycStatus));
    }
}
