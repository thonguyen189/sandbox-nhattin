using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using NhatTinSandbox.Infrastructure.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var cs = builder.Configuration.GetConnectionString("Sandbox")
         ?? "Data Source=../NhatTinSandbox.Api/App_Data/nhattin-sandbox.db";
builder.Services.AddDbContext<SandboxDbContext>(o => o.UseSqlite(cs));
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IWebhookDispatcher, HttpWebhookDispatcher>();
builder.Services.AddHttpClient("webhook");

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Bills"));
app.Run();
