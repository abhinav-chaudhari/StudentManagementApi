using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using WebApplication1.Configuration;
using WebApplication1.Data;
using WebApplication1.Errors;
using WebApplication1.Middlerware;
using WebApplication1.Security;
using WebApplication1.Services;
using WebApplication1.Setup;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Paste the access token returned by login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IBranchService, BranchService>();
//builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("StudentDb")));
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var settings = services
        .GetRequiredService<IOptions<ConnectionStringOptions>>()
        .Value;

    var connection = new SqliteConnectionStringBuilder(
        settings.StudentDb);

    if (connection.Mode != SqliteOpenMode.Memory &&
        !string.IsNullOrWhiteSpace(connection.DataSource) &&
        connection.DataSource != ":memory:" &&
        !Path.IsPathRooted(connection.DataSource))
    {
        connection.DataSource = Path.GetFullPath(
            connection.DataSource,
            builder.Environment.ContentRootPath);
    }

    options.UseSqlite(connection.ConnectionString);
});
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options => options.User.RequireUniqueEmail = true).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAuthorization(options => { options.AddPolicy(AuthorizationPolicies.ManageSchoolData, policy => { policy.RequireAuthenticatedUser(); policy.RequireRole("Admin"); }); });
builder.Services.AddOptions<ConnectionStringOptions>().Bind(builder.Configuration.GetSection(ConnectionStringOptions.SectionName)).Validate(options => !string.IsNullOrWhiteSpace(options.StudentDb), "ConnectionStrings:StudentDb is required.").ValidateOnStart();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(name: "database", tags: new[] { "ready" });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options => { options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; options.AddPolicy<String>(RateLimitPolicies.StudentRead, context => { string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"; return RateLimitPartition.GetFixedWindowLimiter(partitionKey: clientIp, factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }); }); });
string? keyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
if (!string.IsNullOrWhiteSpace(keyRingPath))
{
    string fullPath = Path.GetFullPath(keyRingPath, builder.Environment.ContentRootPath);
    Directory.CreateDirectory(fullPath);
    builder.Services.AddDataProtection().SetApplicationName("StudentManagementApi").PersistKeysToFileSystem(new DirectoryInfo(fullPath));
}
var app = builder.Build();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

bool developmentBootstrap = app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("AdminBootstrap:Enabled");
bool hostedProvisioning = app.Environment.IsProduction() && app.Configuration.GetValue<bool>("AdminProvisioning:Enabled");

if (developmentBootstrap || hostedProvisioning)
{
    await AdminBootstrapper.RunAsync(app.Services, app.Configuration);
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapGroup("api/auth").MapIdentityApi<IdentityUser>();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
app.Run();
public partial class Program { }