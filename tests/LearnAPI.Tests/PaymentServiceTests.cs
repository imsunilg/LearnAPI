using LearnAPI.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LearnAPI.Tests;

public class PaymentServiceTests
{
    private const string Started = """{"created":true,"payment":{"id":7,"amount":1500.00,"paymentNo":"PAY-2026-000007"}}""";
    private const string Replay = """{"created":false,"payment":{"id":7,"amount":1500.00,"paymentNo":"PAY-2026-000007","status":"Success"}}""";

    private static (PaymentService Svc, FakeJsonDb Db, CountingGateway Gateway) Build(string startedJson, bool gatewaySuccess = true)
    {
        var db = new FakeJsonDb();
        db.Handlers["fn_payment_start"] = _ => startedJson;
        db.Handlers["fn_payment_complete"] = a => $$"""{"id":7,"status":"{{((bool)a[1]! ? "Success" : "Failed")}}"}""";
        var gw = new CountingGateway(gatewaySuccess);
        return (new PaymentService(db, gw, NullLogger<PaymentService>.Instance), db, gw);
    }

    [Fact]
    public async Task New_payment_charges_the_gateway_once_and_stores_the_outcome()
    {
        var (svc, db, gw) = Build(Started);
        var json = await svc.PayAsync(1, "UPI", null, "a@b", "key-1", "tester", CancellationToken.None);

        Assert.Equal(1, gw.Charges);
        Assert.Contains("Success", json);
        Assert.Equal(["fn_payment_start", "fn_payment_complete"], db.Calls.Select(c => c.Function));
    }

    [Fact]
    public async Task Replayed_idempotency_key_never_charges_again()
    {
        var (svc, db, gw) = Build(Replay);
        var json = await svc.PayAsync(1, "UPI", null, "a@b", "key-1", "tester", CancellationToken.None);

        Assert.Equal(0, gw.Charges);                                  // the money was NOT taken twice
        Assert.Single(db.Calls);                                      // fn_payment_complete is not called either
        Assert.Contains("PAY-2026-000007", json);
    }

    [Fact]
    public async Task Gateway_failure_is_recorded_as_failed()
    {
        var (svc, _, _) = Build(Started, gatewaySuccess: false);
        var json = await svc.PayAsync(1, "UPI", null, "fail@upi", "key-2", "tester", CancellationToken.None);
        Assert.Contains("Failed", json);
    }
}
