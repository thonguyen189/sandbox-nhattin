using Microsoft.Extensions.DependencyInjection;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Extensions;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class ClientAndDiTests
{
    [Fact]
    public async Task Standalone_client_creates_bill_end_to_end_via_handler()
    {
        var handler = new StubHttpMessageHandler(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"A\",\"refresh_token\":\"R\"}}")
                : TestResponses.Ok("{\"success\":true,\"data\":{\"bill_code\":\"CP1\"}}"));

        var client = new NhatTinLogisticsClient(
            new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" },
            handler);

        var resp = await client.Bill.CreateAsync(new Types.Requests.CreateBillRequest { Weight = 1 });

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data!.BillCode);
    }

    [Fact]
    public void Di_registration_resolves_client()
    {
        var services = new ServiceCollection();
        services.AddNhatTinLogisticsClient(o =>
        {
            o.Username = "u"; o.Password = "p"; o.BaseUrl = "https://test.local";
        });
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<NhatTinLogisticsClient>();
        Assert.NotNull(client.Auth);
        Assert.NotNull(client.Bill);
        Assert.NotNull(client.Location);
    }

    [Fact]
    public void Di_registration_validates_options()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() =>
            services.AddNhatTinLogisticsClient(o => { /* no username/password, AutoAuthenticate default true */ }));
    }
}
