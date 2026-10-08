namespace LearnAPI.Application.DTOs;

public sealed record BrokerRequest(
    string FirmName,
    string ContactPerson,
    string Email,
    string Mobile,
    string IrdaiRegistrationNo,
    DateOnly RegistrationExpiryDate,
    string? GstNumber,
    string? AddressLine,
    string? City,
    string? State,
    decimal CommissionPercent);
