using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var csTemplate = builder.Configuration.GetConnectionString("Webhooks")
         ?? "Data Source=App_Data/nhattin-webhooks.db";

// Resolve a relative SQLite file path against ContentRootPath (not the process's current
// directory), so the DB lands in the same place under `dotnet run` and under
// WebApplicationFactory-hosted tests, whose current directory differs from ContentRootPath.
var sqliteBuilder = new SqliteConnectionStringBuilder(csTemplate);
if (!Path.IsPathRooted(sqliteBuilder.DataSource))
{
    sqliteBuilder.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteBuilder.DataSource);
}
var cs = sqliteBuilder.ToString();
builder.Services.AddDbContext<WebhookDbContext>(o => o.UseSqlite(cs));

var app = builder.Build();

Directory.CreateDirectory(Path.GetDirectoryName(sqliteBuilder.DataSource)!);
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapRazorPages();
app.Run();

public partial class Program { }
