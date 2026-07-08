using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NhatTinSandbox.Api.Extensions;
using NhatTinSandbox.Api.Json;
using NhatTinSandbox.Infrastructure.Auth;
using NhatTinSandbox.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// snake_case for BOTH inbound binding (s_name -> SName) and outbound JSON,
// matching NhatTinAPIDocumentation/vi/ field names. Net6.0 port of the .NET 8
// built-in JsonNamingPolicy.SnakeCaseLower (see SnakeCaseLowerNamingPolicy).
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = SnakeCaseLowerNamingPolicy.Instance;
    o.JsonSerializerOptions.DictionaryKeyPolicy = SnakeCaseLowerNamingPolicy.Instance;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSandboxInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Migrate, seed.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SandboxDbContext>();
    db.Database.Migrate();
    SeedData.EnsureSeeded(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
