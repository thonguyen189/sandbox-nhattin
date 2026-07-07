using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public sealed class BillApi : IBillApi
{
    private readonly NhatTinHttpClient _http;
    private readonly NhatTinLogisticsClientOptions _options;

    public BillApi(NhatTinHttpClient http, NhatTinLogisticsClientOptions options)
    {
        _http = http;
        _options = options;
    }

    public Task<NhatTinResponse<BillResult>> CreateAsync(CreateBillRequest request, CancellationToken ct = default)
        => _http.PostAsync<BillResult>("/v3/bill/create", request, ct);

    public Task<NhatTinResponse<BillResult>> UpdateAsync(UpdateBillRequest request, CancellationToken ct = default)
    {
        request.PartnerId ??= _options.PartnerId;
        return _http.PostAsync<BillResult>("/v3/bill/update-shipping", request, ct);
    }

    public Task<NhatTinResponse<CancelResponse>> CancelAsync(IEnumerable<string> billCodes, CancellationToken ct = default)
        => _http.PostAsync<CancelResponse>("/v3/bill/destroy", new { bill_code = billCodes.ToArray() }, ct);

    public Task<NhatTinResponse<List<FeeOption>>> CalcFeeAsync(CalcFeeRequest request, CancellationToken ct = default)
    {
        request.PartnerId ??= _options.PartnerId;
        return _http.PostAsync<List<FeeOption>>("/v3/bill/calc-fee", request, ct);
    }

    public Task<NhatTinResponse<RevertResult>> RevertAsync(IEnumerable<string> billCodes, CancellationToken ct = default)
        => _http.PostAsync<RevertResult>("/v3/bill/revert-bill", new { bill_code = billCodes.ToArray() }, ct);

    public Task<NhatTinResponse<List<TrackingResult>>> TrackingAsync(string billCode, CancellationToken ct = default)
        => _http.GetAsync<List<TrackingResult>>($"/v3/bill/tracking?bill_code={Uri.EscapeDataString(billCode)}", ct);

    public string GetPrintUrl(string billCode, int? partnerId = null)
    {
        var pid = partnerId ?? _options.PartnerId
            ?? throw new ArgumentException("PartnerId is required for printing. Set Options.PartnerId or pass partnerId.");
        var baseUrl = _options.ResolveBaseUrl();
        return $"{baseUrl}/v3/bill/print?do_code={Uri.EscapeDataString(billCode)}&partner_id={pid}";
    }

    public Task<PrintResult> PrintAsync(string billCode, int? partnerId = null, CancellationToken ct = default)
        // Content-type-aware: NhatTin returns HTTP 200 for both success (HTML) and JSON error envelopes,
        // so inspect PrintResult.Success rather than the HTTP status.
        => _http.GetPrintAsync(GetPrintUrl(billCode, partnerId), ct);
}
