using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

/// <summary>Mirrors LearnWEB src/features/motor/motorSchema.ts and the ck_vehicles_* constraints.</summary>
public sealed class VehicleRequestValidator : AbstractValidator<VehicleRequest>
{
    public static readonly string[] VehicleTypes = ["CAR", "TWO_WHEELER", "COMMERCIAL"];
    public const string RegistrationPattern = @"^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$";

    public VehicleRequestValidator()
    {
        RuleFor(x => x.RegistrationNo).NotEmpty()
            .Must(r => System.Text.RegularExpressions.Regex.IsMatch(Normalize(r), RegistrationPattern))
            .WithMessage("Registration number must look like MH12AB1234.");
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Select a customer.");
        RuleFor(x => x.VehicleType).Must(t => VehicleTypes.Contains(t)).WithMessage("Unknown vehicle type.");
        RuleFor(x => x.Make).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Variant!).MaximumLength(60).When(x => x.Variant is not null);
        RuleFor(x => x.FuelType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ManufactureYear).InclusiveBetween(1990, DateTime.Today.Year + 1);
        RuleFor(x => x.RegistrationDate!.Value).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).When(x => x.RegistrationDate.HasValue)
            .WithMessage("Registration date cannot be in the future.");
        RuleFor(x => x.CubicCapacity).InclusiveBetween(1, 20000);
        RuleFor(x => x.SeatingCapacity!.Value).InclusiveBetween(1, 60).When(x => x.SeatingCapacity.HasValue);
        RuleFor(x => x.EngineNo!).MaximumLength(30).When(x => x.EngineNo is not null);
        RuleFor(x => x.ChassisNo!).MaximumLength(30).When(x => x.ChassisNo is not null);
    }

    public static string Normalize(string? reg) => (reg ?? "").Replace(" ", "").Replace("-", "").ToUpperInvariant();
}
