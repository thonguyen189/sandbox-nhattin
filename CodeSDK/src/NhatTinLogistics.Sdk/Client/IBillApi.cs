using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public interface IBillApi
{
    Task<NhatTinResponse<BillResult>> CreateAsync(CreateBillRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<BillResult>> UpdateAsync(UpdateBillRequest request, CancellationToken ct = default);
    // Tasks 8 and 9 extend this interface (calc-fee/cancel/revert/tracking, then print).
}
