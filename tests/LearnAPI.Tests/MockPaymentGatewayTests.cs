using LearnAPI.Infrastructure.Payments;

namespace LearnAPI.Tests;

public class MockPaymentGatewayTests
{
    private static Task<GatewayResult> Charge(string mode, string? reference) =>
        new MockPaymentGateway().ChargeAsync(new GatewayRequest(mode, 1000m, reference, "PAY-1"), CancellationToken.None);

    [Theory]
    [InlineData("UPI", "fail@upi", false)]
    [InlineData("UPI", "FAIL@UPI", false)]
    [InlineData("UPI", "nobank", false)]        // no @ = invalid UPI id
    [InlineData("UPI", "asha@okbank", true)]
    [InlineData("CARD", "4242", true)]
    [InlineData("CARD", "0000", false)]          // declined
    [InlineData("CARD", "1111", false)]          // insufficient funds
    [InlineData("CARD", "12", false)]            // too short
    [InlineData("NETBANKING", "DOWN", false)]
    [InlineData("NETBANKING", "HDFC", true)]
    [InlineData("CASH", null, true)]
    [InlineData("CHEQUE", "123456", true)]
    public async Task Outcome_follows_the_documented_rules(string mode, string? reference, bool expected)
    {
        var result = await Charge(mode, reference);
        Assert.Equal(expected, result.Success);
    }

    [Fact]
    public async Task Every_attempt_gets_a_transaction_reference()
    {
        var a = await Charge("UPI", "asha@okbank");
        var b = await Charge("UPI", "asha@okbank");
        Assert.StartsWith("TXN", a.TransactionRef);
        Assert.NotEqual(a.TransactionRef, b.TransactionRef);
    }
}
