using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public interface IBillApi
{
    Task<NhatTinResponse<BillResult>> CreateAsync(CreateBillRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<BillResult>> UpdateAsync(UpdateBillRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<List<CancelResult>>> CancelAsync(IEnumerable<string> billCodes, CancellationToken ct = default);
    Task<NhatTinResponse<List<FeeOption>>> CalcFeeAsync(CalcFeeRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<RevertResult>> RevertAsync(IEnumerable<string> billCodes, CancellationToken ct = default);
    Task<NhatTinResponse<List<TrackingResult>>> TrackingAsync(string billCode, CancellationToken ct = default);
    string GetPrintUrl(string billCode, int? partnerId = null);
    Task<PrintResult> PrintAsync(string billCode, int? partnerId = null, CancellationToken ct = default);
}
