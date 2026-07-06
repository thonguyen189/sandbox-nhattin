using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Common;
using NhatTinSandbox.Application.Webhooks;

namespace NhatTinSandbox.Api.Controllers;

// Internal sandbox helpers — NOT part of the real Nhất Tín API surface.
[ApiController]
[Route("sandbox")]
public sealed class SandboxController : ControllerBase
{
    private readonly IBillService _bills;
    private readonly IWebhookDispatcher _dispatcher;

    public SandboxController(IBillService bills, IWebhookDispatcher dispatcher)
    {
        _bills = bills;
        _dispatcher = dispatcher;
    }

    public sealed record SimulateStatusBody(int status_id, string? reason);

    [HttpPost("bills/{billCode}/simulate-status")]
    public async Task<IActionResult> SimulateStatus(string billCode, [FromBody] SimulateStatusBody body, CancellationToken ct)
    {
        var updated = await _bills.SetStatusAsync(billCode, body.status_id, body.reason, ct);
        if (updated is null) return NotFound(ApiResult.Fail("Bill not found"));
        await _dispatcher.DispatchAsync(updated.BillId, ct);
        return Ok(ApiResult.Ok(new { bill_code = updated.BillCode, status_id = updated.StatusId }, "Status simulated and webhook dispatched"));
    }
}
