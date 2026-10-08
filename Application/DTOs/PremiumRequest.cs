using System.Text.Json;

namespace LearnAPI.Application.DTOs;

/// <summary>Product + the details the product form collected (vehicle/insurance for motor, members for health...).</summary>
public sealed record PremiumRequest(long ProductId, JsonElement Details, long? CustomerId, bool Save = false);
