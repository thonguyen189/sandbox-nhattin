using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NhatTinMvc.Web.Data;
using NhatTinMvc.Web.Hubs;

namespace NhatTinMvc.Tests;

/// <summary>Test helpers: mỗi test một InMemory DB riêng (độc lập, không chia sẻ state).</summary>
internal static class TestDb
{
    public static MvcDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MvcDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MvcDbContext(options);
    }
}

/// <summary>
/// Fake IHubContext để WebhookIngestService đẩy SignalR mà không cần hạ tầng thật.
/// Clients.All.BillStatusChanged(...) chạy no-op; các thành viên không dùng ném NotImplementedException.
/// </summary>
internal sealed class FakeHubContext : IHubContext<BillStatusHub, IBillStatusClient>
{
    public IHubClients<IBillStatusClient> Clients { get; } = new FakeClients();
    public IGroupManager Groups { get; } = new FakeGroups();
}

internal sealed class FakeClients : IHubClients<IBillStatusClient>
{
    public IBillStatusClient All { get; } = new NoopClient();

    public IBillStatusClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
    public IBillStatusClient Client(string connectionId) => throw new NotImplementedException();
    public IBillStatusClient Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
    public IBillStatusClient Group(string groupName) => throw new NotImplementedException();
    public IBillStatusClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
    public IBillStatusClient Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
    public IBillStatusClient User(string userId) => throw new NotImplementedException();
    public IBillStatusClient Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
}

internal sealed class NoopClient : IBillStatusClient
{
    public Task BillStatusChanged(BillStatusUpdate update) => Task.CompletedTask;
}

internal sealed class FakeGroups : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
