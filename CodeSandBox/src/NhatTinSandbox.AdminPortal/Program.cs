using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using NhatTinSandbox.Infrastructure.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Services.AddRazorPages();

var cs = builder.Configuration.GetConnectionString("Sandbox")
         ?? "Server=192.168.200.8;Database=NhatTinSandbox;User Id=vipos;Password=CHANGE_ME;TrustServerCertificate=True;Encrypt=False";
builder.Services.AddDbContext<SandboxDbContext>(o => o.UseSqlServer(cs));
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IWebhookDispatcher, HttpWebhookDispatcher>();
builder.Services.AddHttpClient("webhook");

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Bills"));
app.Run();
