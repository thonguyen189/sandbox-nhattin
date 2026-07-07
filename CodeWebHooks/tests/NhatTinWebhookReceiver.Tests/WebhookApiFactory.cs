using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NhatTinWebhookReceiver.Api.Persistence;

namespace NhatTinWebhookReceiver.Tests;

public class WebhookApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<WebhookDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(WebhookDbContext)).ToList();
            foreach (var d in toRemove) services.Remove(d);
            services.AddDbContext<WebhookDbContext>(o => o.UseInMemoryDatabase("webhook-tests"));
        });
    }
}
