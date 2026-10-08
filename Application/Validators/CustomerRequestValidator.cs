using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

/// <summary>Rules mirror the Zod schema in LearnWEB (src/features/customers/customerSchema.ts) and the ck_customers_* DB constraints - keep all three in sync.</summary>
public sealed class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    public static readonly string[] Genders = ["Male", "Female", "Other"];
    public static readonly string[] KycStatuses = ["Pending", "Verified", "Rejected"];

    public CustomerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^[6-9]\d{9}$").WithMessage("Enter a valid 10-digit mobile number");
        RuleFor(x => x.DateOfBirth)
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today).AddYears(-18)).WithMessage("Customer must be at least 18 years old");
        RuleFor(x => x.Gender!).Must(g => Genders.Contains(g)).When(x => !string.IsNullOrEmpty(x.Gender));
        RuleFor(x => x.PanNumber!).Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]$").WithMessage("PAN must look like ABCDE1234F")
            .When(x => !string.IsNullOrEmpty(x.PanNumber));
        RuleFor(x => x.AddressLine!).MaximumLength(250).When(x => x.AddressLine is not null);
        RuleFor(x => x.City!).MaximumLength(80).When(x => x.City is not null);
        RuleFor(x => x.State!).MaximumLength(80).When(x => x.State is not null);
        RuleFor(x => x.Pincode!).Matches(@"^\d{6}$").WithMessage("Pincode must be 6 digits").When(x => !string.IsNullOrEmpty(x.Pincode));
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required when a pincode is given").When(x => !string.IsNullOrEmpty(x.Pincode));
        RuleFor(x => x.KycStatus!).Must(k => KycStatuses.Contains(k)).When(x => !string.IsNullOrEmpty(x.KycStatus));
    }
}
