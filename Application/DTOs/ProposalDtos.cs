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
    JsonElement Details,
    /// <summary>Wizard "save & exit" before the form is complete: stored without a price, cannot be submitted.</summary>
    bool Partial = false);

public sealed record DecisionRequest(string Decision, string? Remarks);

public sealed record CancelRequest(string? Remarks);

/// <summary>Unified submission: ProposalId is set when a saved draft is being completed.</summary>
public sealed record SubmitProposalRequest(long? ProposalId, ProposalRequest Proposal);
