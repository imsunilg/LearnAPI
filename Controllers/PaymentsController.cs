using LearnAPI.Application.Interfaces;
using LearnAPI.Application.Services;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

public sealed record PayRequest(long PolicyId, string Mode, decimal? Amount, string? Reference);
public sealed record RefundRequest(string? Remarks);

[Route("api/payments")]
public class PaymentsController(ICrudService service, IJsonDb db, PaymentService payments) : CrudControllerBase(service)
{
    protected override string Entity => "payment";
    public override string PermissionModule => "payments";

    /// <summary>Pay a policy premium. Requires an Idempotency-Key header (a UUID the client generates per payment attempt).</summary>
    [HttpPost("pay")]
    public async Task<IActionResult> Pay(PayRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 80)
            throw new BusinessRuleException("An Idempotency-Key header (max 80 characters) is required.");
        return JsonResult(await payments.PayAsync(request.PolicyId, request.Mode, request.Amount, request.Reference, idempotencyKey, CurrentUser, ct), StatusCodes.Status201Created);
    }

    [HttpPost("{id:long}/refund")]
    [HasPermission("payments.refund")]
    public async Task<IActionResult> Refund(long id, RefundRequest request, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_payment_refund", ct, id, request.Remarks, CurrentUser));
}
