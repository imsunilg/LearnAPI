using System.Text.Json;
using System.Text.Json.Nodes;
using LearnAPI.Application.Interfaces;
using LearnAPI.Infrastructure.Payments;

namespace LearnAPI.Application.Services;

/// <summary>
/// Payment orchestration: record the attempt (idempotent) → charge the gateway → store the outcome.
/// The idempotency key makes a double-click or a network retry safe: the second call returns the first result and never charges again.
/// </summary>
public sealed class PaymentService(IJsonDb db, IPaymentGateway gateway, ILogger<PaymentService> logger)
{
    public async Task<string> PayAsync(long policyId, string mode, decimal? amount, string? reference, string idempotencyKey, string user, CancellationToken ct)
    {
        var started = JsonNode.Parse((await db.CallAsync("fn_payment_start", ct, policyId, mode, amount, idempotencyKey, user))!)!;
        var payment = started["payment"]!;
        if (!started["created"]!.GetValue<bool>())
        {
            logger.LogInformation("Idempotent replay for key {Key}", idempotencyKey);
            return payment.ToJsonString();
        }

        var id = payment["id"]!.GetValue<long>();
        var result = await gateway.ChargeAsync(new GatewayRequest(mode, payment["amount"]!.GetValue<decimal>(), reference, payment["paymentNo"]!.GetValue<string>()), ct);
        return (await db.CallAsync("fn_payment_complete", ct, id, result.Success, result.TransactionRef, new Json(result.Raw.ToJsonString()), user))!;
    }
}
