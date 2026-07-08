using Microsoft.EntityFrameworkCore;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Extensions;
using NhatTinMvc.Web.Data;
using NhatTinMvc.Web.Hubs;
using NhatTinMvc.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// appsettings.Local.json (git-ignored) đè placeholder: conn-string SQL thật + creds sandbox.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
var cfg = builder.Configuration;

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDbContext<MvcDbContext>(opt =>
    opt.UseSqlServer(cfg.GetConnectionString("MvcDb")));

// SDK tiêu thụ dạng gói — manual token mode, trỏ sandbox nội bộ (:5080), KHÔNG phải NTL thật.
builder.Services.AddNhatTinLogisticsClient(o =>
{
    o.BaseUrl = cfg["NhatTin:BaseUrl"];
    o.AutoAuthenticate = false;
    o.Environment = NhatTinEnvironment.Sandbox;
});

builder.Services.AddSingleton<DemoAuthState>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IWebhookIngestService, WebhookIngestService>();
builder.Services.AddHttpClient<ISandboxControl, SandboxControlClient>(c =>
    c.BaseAddress = new Uri(cfg["Sandbox:BaseUrl"] ?? "http://localhost:5080"));

var app = builder.Build();

// In rõ host NhatTin đang trỏ tới (local emulator hay sandbox thật) để tránh nhầm môi trường.
var ntOptions = app.Services.GetRequiredService<NhatTinLogisticsClientOptions>();
app.Logger.LogInformation("NhatTin API host = {Host} (Environment={Env}, BaseUrl override='{BaseUrl}')",
    ntOptions.ResolveBaseUrl(), ntOptions.Environment, cfg["NhatTin:BaseUrl"]);

// Tự tạo/migrate DB NhatTinMvc khi có conn-string thật (login vipos đủ quyền dbcreator).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MvcDbContext>();
    var cs = cfg.GetConnectionString("MvcDb");
    if (!string.IsNullOrWhiteSpace(cs) && cs != "CHANGE_ME")
    {
        try { db.Database.Migrate(); }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Không migrate được DB NhatTinMvc — kiểm tra appsettings.Local.json và quyền SQL.");
        }
    }
    else
    {
        app.Logger.LogWarning("ConnectionStrings:MvcDb chưa cấu hình (CHANGE_ME). Tạo appsettings.Local.json trước khi dùng DB.");
    }
}

// Không dùng HTTPS redirect: sandbox dispatch webhook qua http://localhost:5110, redirect sẽ làm rớt POST.
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<BillStatusHub>("/hubs/bill-status");

app.Run();
