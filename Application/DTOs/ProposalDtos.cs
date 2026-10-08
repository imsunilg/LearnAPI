using System.Text.Json;

namespace LearnAPI.Application.DTOs;

/// <summary>Create/update a DRAFT proposal. The premium sent by a client (if any) is ignored - it is recalculated in the database.</summary>
public sealed record ProposalRequest(
    long CustomerId,
    long ProductId,
    DateOnly? StartDate,
    int PolicyTermYears,
    long? AgentId,
    long? BrokerId,
    string? Remarks,
    JsonElement Details);

public sealed record DecisionRequest(string Decision, string? Remarks);

public sealed record CancelRequest(string? Remarks);
