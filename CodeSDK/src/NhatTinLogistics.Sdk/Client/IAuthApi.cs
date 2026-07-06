using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public interface IAuthApi
{
    Task<NhatTinResponse<AuthToken>> SignInAsync(string username, string password, CancellationToken ct = default);
    Task<NhatTinResponse<AuthToken>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
