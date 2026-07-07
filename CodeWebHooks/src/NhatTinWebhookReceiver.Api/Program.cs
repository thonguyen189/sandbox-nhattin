using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var cs = builder.Configuration.GetConnectionString("Webhooks")
         ?? "Server=192.168.200.8;Database=NhatTinWebhooks;User Id=vipos;Password=CHANGE_ME;TrustServerCertificate=True;Encrypt=False";
builder.Services.AddDbContext<WebhookDbContext>(o => o.UseSqlServer(cs));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    if (db.Database.IsRelational()) db.Database.Migrate();
    else db.Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapRazorPages();
app.Run();

public partial class Program { }
