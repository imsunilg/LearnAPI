namespace LearnAPI.Application.DTOs;

public sealed record VehicleRequest(
    string RegistrationNo,
    long CustomerId,
    string VehicleType,
    string Make,
    string Model,
    string? Variant,
    string FuelType,
    int ManufactureYear,
    DateOnly? RegistrationDate,
    string? EngineNo,
    string? ChassisNo,
    int CubicCapacity,
    int? SeatingCapacity);
