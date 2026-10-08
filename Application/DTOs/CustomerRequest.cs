namespace LearnAPI.Application.DTOs;

public sealed record CustomerRequest(
    string FullName,
    string Email,
    string Mobile,
    DateOnly DateOfBirth,
    string? Gender,
    string? PanNumber,
    string? AddressLine,
    string? City,
    string? State,
    string? Pincode,
    string? KycStatus);
