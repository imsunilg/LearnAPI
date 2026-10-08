using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace LearnAPI.Infrastructure.Payments;

public sealed record GatewayRequest(string Mode, decimal Amount, string? Reference, string PaymentNo);
public sealed record GatewayResult(bool Success, string TransactionRef, string Message, JsonObject Raw);

public interface IPaymentGateway
{
    Task<GatewayResult> ChargeAsync(GatewayRequest request, CancellationToken ct);
}

/// <summary>
/// A pretend payment gateway for the learning project. Behaviour is deterministic so failures can be demonstrated:
///   UPI: reference "fail@upi" fails.   CARD: last digits 0000 = declined, 1111 = insufficient funds.   NETBANKING: reference "DOWN" = bank down.
/// Everything else succeeds. CASH and CHEQUE are recorded manually and always succeed.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    public async Task<GatewayResult> ChargeAsync(GatewayRequest r, CancellationToken ct)
    {
        await Task.Delay(700, ct); // network latency
        var reference = (r.Reference ?? "").Trim();
        var txn = "TXN" + Convert.ToHexString(RandomNumberGenerator.GetBytes(5));

        (bool ok, string msg) = r.Mode.ToUpperInvariant() switch
        {
            "UPI" when reference.Equals("fail@upi", StringComparison.OrdinalIgnoreCase) => (false, "UPI request declined by the customer's bank."),
            "UPI" when !reference.Contains('@') => (false, "Invalid UPI id."),
            "CARD" when reference.EndsWith("0000") => (false, "Card declined by the issuing bank."),
            "CARD" when reference.EndsWith("1111") => (false, "Insufficient funds."),
            "CARD" when reference.Length < 4 => (false, "Enter the last 4 digits of the card."),
            "NETBANKING" when reference.Equals("DOWN", StringComparison.OrdinalIgnoreCase) => (false, "The bank's net-banking service is not responding."),
            _ => (true, "Payment successful."),
        };

        var raw = new JsonObject { ["gateway"] = "MockPay", ["transactionRef"] = txn, ["message"] = msg, ["mode"] = r.Mode, ["amount"] = r.Amount, ["approved"] = ok };
        return new GatewayResult(ok, txn, msg, raw);
    }
}
