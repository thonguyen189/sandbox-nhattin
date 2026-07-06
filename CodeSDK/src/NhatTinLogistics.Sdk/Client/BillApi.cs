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
}
