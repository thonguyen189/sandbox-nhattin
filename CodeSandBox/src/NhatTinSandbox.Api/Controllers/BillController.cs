using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Common;

namespace NhatTinSandbox.Api.Controllers;

[ApiController]
[Authorize]
public sealed class BillController : ControllerBase
{
    private readonly IBillService _bills;
    public BillController(IBillService bills) => _bills = bills;

    [HttpPost("/v3/bill/create")]
    public async Task<IActionResult> Create([FromBody] CreateBillInput input, CancellationToken ct)
    {
        var bill = await _bills.CreateAsync(input, ct);
        return Ok(ApiResult.Ok(ToBillData(bill), "Create bill successfully"));
    }

    [HttpPost("/v3/bill/update-shipping")]
    public async Task<IActionResult> Update([FromBody] UpdateBillInput input, CancellationToken ct)
    {
        var bill = await _bills.UpdateAsync(input, ct);
        if (bill is null) return Ok(ApiResult.Fail("Bill not found"));
        return Ok(ApiResult.Ok(ToBillData(bill), "Update successful"));
    }

    [HttpPost("/v3/bill/calc-fee")]
    public async Task<IActionResult> CalcFee([FromBody] CalcFeeInput input, CancellationToken ct)
    {
        var options = await _bills.CalcFeeAsync(input, ct);
        return Ok(ApiResult.Ok(options.Select(ToFeeData)));
    }

    public sealed record BillCodeList(List<string> bill_code);

    [HttpPost("/v3/bill/destroy")]
    public async Task<IActionResult> Destroy([FromBody] BillCodeList body, CancellationToken ct)
    {
        var results = await _bills.CancelAsync(body.bill_code, ct);
        return Ok(ApiResult.Ok(results.Select(r => new { doCode = r.DoCode, message = r.Message })));
    }

    [HttpPost("/v3/bill/revert-bill")]
    public async Task<IActionResult> Revert([FromBody] BillCodeList body, CancellationToken ct)
    {
        var result = await _bills.RevertAsync(body.bill_code, ct);
        return Ok(ApiResult.Ok(new { success = result.Success, failed = result.Failed }));
    }

    [HttpGet("/v3/bill/tracking")]
    public async Task<IActionResult> Tracking([FromQuery(Name = "bill_code")] string billCode, CancellationToken ct)
    {
        var bill = await _bills.GetByCodeAsync(billCode, ct);
        if (bill is null) return Ok(ApiResult.Fail("Bill not found"));
        return Ok(ApiResult.Ok(new[] { ToTrackingData(bill) }, "Tracking successfully"));
    }

    [HttpGet("/v3/bill/print")]
    public IActionResult Print([FromQuery(Name = "do_code")] string doCode, [FromQuery(Name = "partner_id")] int partnerId)
    {
        // Real print response format is unconfirmed; sandbox returns a placeholder link.
        var url = $"http://localhost:5080/sandbox/labels/{doCode}.html";
        return Ok(ApiResult.Ok(new { do_code = doCode, partner_id = partnerId, print_url = url }, "Sandbox print placeholder"));
    }

    private static object ToBillData(BillSummary b) => new
    {
        bill_id = b.BillId,
        bill_code = b.BillCode,
        ref_code = b.RefCode ?? "",
        status_id = b.StatusId,
        cod_amount = b.CodAmount,
        service_id = b.ServiceId,
        payment_method = b.PaymentMethod,
        created_at = b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        main_fee = b.MainFee,
        total_fee = b.TotalFee,
        receiver_name = b.ReceiverName,
        receiver_phone = b.ReceiverPhone,
        receiver_address = b.ReceiverAddress,
        package_no = b.PackageNo,
        weight = b.Weight,
        cargo_content = b.CargoContent ?? "",
        cargo_value = b.CargoValue,
        note = b.Note ?? ""
    };

    private static object ToFeeData(FeeOption f) => new
    {
        weight = f.Weight,
        total_fee = f.TotalFee,
        main_fee = f.MainFee,
        insur_fee = f.InsurFee,
        remote_fee = f.RemoteFee,
        cod_fee = f.CodFee,
        service_id = f.ServiceId,
        service_name = f.ServiceName,
        lead_time = f.LeadTime
    };

    private static object ToTrackingData(BillSummary b) => new
    {
        bill_code = b.BillCode,
        ref_code = b.RefCode ?? "",
        weight = b.Weight.ToString("0.00"),
        bill_status_id = b.StatusId,
        cod_amt = b.CodAmount.ToString("0.00"),
        total_fee = b.TotalFee.ToString("0"),
        receiver_name = b.ReceiverName,
        receiver_phone = b.ReceiverPhone,
        receiver_address = b.ReceiverAddress,
        note = b.Note ?? ""
    };
}
