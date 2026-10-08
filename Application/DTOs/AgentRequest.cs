namespace LearnAPI.Application.DTOs;

public sealed record AgentRequest(
    string FullName,
    string Email,
    string Mobile,
    string LicenseNumber,
    DateOnly LicenseExpiryDate,
    decimal CommissionPercent,
    long? BrokerId,
    string? City,
    string? State,
    string? Status,
    DateOnly? JoinedDate);
