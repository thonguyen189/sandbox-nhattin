using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Common;
using NhatTinSandbox.Application.Locations;

namespace NhatTinSandbox.Api.Controllers;

[ApiController]
[Authorize]
public sealed class LocationController : ControllerBase
{
    private readonly ILocationCatalog _catalog;
    public LocationController(ILocationCatalog catalog) => _catalog = catalog;

    [HttpGet("/v3/loc/provinces")]
    public async Task<IActionResult> Provinces([FromQuery(Name = "is_new")] int? isNew, CancellationToken ct)
        => Ok(ApiResult.Ok(await _catalog.GetProvincesAsync(isNew == 1, ct)));

    [HttpGet("/v3/loc/districts")]
    public async Task<IActionResult> Districts([FromQuery(Name = "province_id")] string provinceId, CancellationToken ct)
        => Ok(ApiResult.Ok(await _catalog.GetDistrictsAsync(provinceId, ct)));

    [HttpGet("/v3/loc/wards")]
    public async Task<IActionResult> Wards(
        [FromQuery(Name = "district_id")] string? districtId,
        [FromQuery(Name = "province_id")] string? provinceId,
        [FromQuery(Name = "is_new")] int? isNew,
        CancellationToken ct)
        => Ok(ApiResult.Ok(await _catalog.GetWardsAsync(districtId, provinceId, isNew == 1, ct)));
}
